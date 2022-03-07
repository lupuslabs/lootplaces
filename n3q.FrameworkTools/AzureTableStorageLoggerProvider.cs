#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

using n3q.Tools;

namespace n3q.FrameworkTools
{
    public class AzureTableStorageLoggerConfig
    {
        public string ConnectionString { get; set; } = "AzureTableStorageLoggerConnectionString";
        public string TableName { get; set; } = "n3qLog";
        public double BufferTimeSec { get; set; } = 2.0;
        public int BatchSize { get; set; } = 100; // max 100
    }

    public class AzureTableStorageLogger : ILogger
    {
        public ICallbackLogger Logger { get; set; } = new ConsoleCallbackLogger();

        readonly AzureTableStorageLoggerProvider _azureTableStorageLoggerProvider;
        readonly AzureTableStorageLoggerSink _sink;
        readonly string _instance;
        readonly string _category;

        public AzureTableStorageLogger(AzureTableStorageLoggerProvider azureTableStorageLoggerProvider, AzureTableStorageLoggerSink sink, string instance, string category)
        {
            _azureTableStorageLoggerProvider = azureTableStorageLoggerProvider;
            _sink = sink;
            _instance = instance;
            _category = category;
        }

        public class Scope : IDisposable
        {
            readonly AzureTableStorageLogger _azureTableStorageLogger;
            public Scope(AzureTableStorageLogger azureTableStorageLogger) { _azureTableStorageLogger = azureTableStorageLogger; }
            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state) { return new Scope(this); }
        public bool IsEnabled(LogLevel logLevel) { return true; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try {

                if (state is IReadOnlyList<KeyValuePair<string, object>> logList) {
                    if (logList.Count > 0) {
                        var line = formatter?.Invoke(state, exception);
                        if (line != null) {
                            _sink.AddLogLine(_instance, _category, logLevel, line);
                        }
                    }
                }

            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        internal void OnDispose()
        {
            _sink.AddLogLine(_instance, _category, LogLevel.Information, $"{_category} {nameof(OnDispose)}", flush: true);
        }
    }

    public class AzureTableStorageLoggerSink
    {
        public ICallbackLogger Logger { get; set; } = new ConsoleCallbackLogger();

        readonly AzureTableStorageLoggerProvider _azureTableStorageLoggerProvider;
        readonly object _mutex = new object();

        List<LogLine> _lines = new List<LogLine>();
        DateTime _lastWrite = DateTime.MinValue;
        CloudTable? _table;
        int _batchCount = 0;

        ITimeProvider TimeProvider => _azureTableStorageLoggerProvider.TimeProvider;
        AzureTableStorageLoggerConfig Config => _azureTableStorageLoggerProvider.Config;

        public AzureTableStorageLoggerSink(AzureTableStorageLoggerProvider azureTableStorageLoggerProvider)
        {
            _azureTableStorageLoggerProvider = azureTableStorageLoggerProvider;
        }

        class LogLine
        {
            public readonly DateTime Time;
            public readonly int Batch;
            public readonly string Instance;
            public readonly string Category;
            public readonly LogLevel Level;
            public readonly string Text;

            public LogLine(DateTime time, int batch, string instance, string category, LogLevel level, string text)
            {
                Time = time;
                Batch = batch;
                Instance = instance;
                Category = category;
                Level = level;
                Text = text;
            }
        }

        public void AddLogLine(string instance, string category, LogLevel level, string line, bool flush = false)
        {
            var now = TimeProvider.UtcNow();

            List<LogLine>? linesToWrite = null;

            if (line.Contains(AzureTableStorageLoggerProvider.FlushLogCommand)) {
                flush = true;
            }

            lock (_mutex) {
                _lines.Add(new LogLine(now, _batchCount, instance, category, level, line));

                if (flush || _lines.Count >= Config.BatchSize || (now - _lastWrite).TotalSeconds >= Config.BufferTimeSec) {
                    linesToWrite = _lines;
                    _lines = new List<LogLine>();

                    _lastWrite = now;

                    _batchCount++;
                    if (_batchCount >= 1000) _batchCount = 0;
                }
            }

            if (linesToWrite != null) {
                _ = Write(linesToWrite);
            }
        }

        async Task Write(List<LogLine> lines)
        {
            var batchOp = new TableBatchOperation();

            try {
                var now = TimeProvider.UtcNow();
                var partitionKey = IntervalDate.FormatStartOfDay(now);

                var count = 0L;
                foreach (var line in lines) {
                    //var rk = kv.Key + $"-{count++}";
                    var rowKey = $"{line.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}.{line.Time.TicksOfSecond():D7}-{line.Batch:D3}-{count++:D3}";

                    var entity = new DynamicTableEntity(partitionKey, rowKey);

                    entity["level"] = new EntityProperty(line.Level.ToString());
                    entity["instance"] = new EntityProperty(line.Instance);
                    entity["category"] = new EntityProperty(line.Category);

                    var gotJson = false;
                    if (line.Text.StartsWith("{")) {
                        try {
                            var node = JsonPath.Node.FromJson(line.Text);

                            var context = (string)node[LogData.Key.Context];
                            if (Is.Value(context)) { entity[LogData.Key.Context] = new EntityProperty(context); }

                            var message = (string)node[LogData.Key.Message];
                            if (Is.Value(message)) { entity[LogData.Key.Message] = new EntityProperty(message); }

                            var data = node[LogData.Key.Data];
                            if (data.String.Length > 0) {
                                foreach (var (key, value) in data.AsDictionary) {
                                    if (value.IsString) {
                                        entity[key] = new EntityProperty(value.AsString);
                                    } else {
                                        entity[key] = new EntityProperty(value.ToJson());
                                    }
                                }
                            }

                            var values = node[LogData.Key.Values];
                            if (values.String.Length > 0) {
                                foreach (var (key, value) in values.AsDictionary) {
                                    if (value.IsString) {
                                        entity[key] = new EntityProperty(value.AsString);
                                    } else {
                                        entity[key] = new EntityProperty(value.ToJson());
                                    }
                                }
                            }

                            gotJson = true;
                        } catch (Exception ex) {
                            Logger.Error("Exception parsing JSON", ex);
                        }
                    }

                    if (!gotJson) {
                        entity["message"] = new EntityProperty(line.Text);
                    }

                    batchOp.InsertOrReplace(entity);
                }

                await ExecuteBatch(batchOp);
            } catch (Exception ex) {
                Logger.Error("Exception executing batch", ex);

                lock (_tryCreateTableIfNotFoundExceptionMutex) {
                    TryCreateTableIfNotFoundException(ex);
                }

                try {
                    Logger.Info("Retrying batch once");
                    await ExecuteBatch(batchOp);
                } catch (Exception exRetry) {
                    Logger.Error("Exception in retry batch", exRetry);
                }
            }
        }

        private async Task ExecuteBatch(TableBatchOperation batchOp)
        {
            var table = GetTable();
            if (table != null) {
                await table.ExecuteBatchAsync(batchOp);
            }
        }

        readonly object _tryCreateTableIfNotFoundExceptionMutex = new object();

        private bool TryCreateTableIfNotFoundException(Exception ex)
        {
            var created = false;

            if (ex is Microsoft.Azure.Cosmos.Table.StorageException storageEx) {
                if (storageEx.RequestInformation != null) {
                    if (storageEx.RequestInformation.HttpStatusCode == 404) {
                        Logger.Info("Trying to create table");
                        _table = CreateTable();
                        created = true;
                    }
                }
            }

            return created;
        }

        private CloudTable? GetTable()
        {
            if (_table == null) {
                _table = CreateTable();
            }

            return _table;
        }

        private CloudTable? CreateTable()
        {
            var storageAccount = CloudStorageAccount.Parse(Config.ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(Config.TableName);
            try {
                table.CreateIfNotExists(new TableRequestOptions { MaximumExecutionTime = TimeSpan.FromSeconds(20) });
            } catch (Exception ex) {
                Logger.Error(ex);
                table = null;
            }
            return table;
        }

    }

    public class AzureTableStorageLoggerProvider : ILoggerProvider
    {
        public const string FlushLogCommand = "tf5IUh75UtgzNjnjDow8fth_flush";

        public ITimeProvider TimeProvider { get; set; } = new RealTime();
        public AzureTableStorageLoggerConfig Config { get; set; } = new AzureTableStorageLoggerConfig();

        readonly string _instanceName;
        readonly List<AzureTableStorageLogger> _loggers = new List<AzureTableStorageLogger>();
        readonly AzureTableStorageLoggerSink _sink;

        public AzureTableStorageLoggerProvider(string instanceName)
        {
            _instanceName = instanceName;
            _sink = new AzureTableStorageLoggerSink(this);
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new AzureTableStorageLogger(this, _sink, _instanceName, categoryName);
            lock (_loggers) {
                _loggers.Add(logger);
            }
            return logger;
        }

        public void Dispose()
        {
            foreach (var logger in _loggers) {
                logger.OnDispose();
            }
        }
    }
}