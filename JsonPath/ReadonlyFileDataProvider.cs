﻿using System.Threading.Tasks;
using System.IO;
using System;

namespace JsonPath
{
    public class ReadonlyFileDataProvider : IDataProvider
    {
        private readonly string _basePath;

        public ReadonlyFileDataProvider(string basePath = null)
        {
            _basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/i18n");
        }

        public string FilePath(string id)
        {
            return Path.Combine(_basePath, id) + ".json";
        }

        public bool HasData(string id)
        {
            return File.Exists(FilePath(id));
        }

        public void SetData(string id, string value)
        {
            throw new Exception("Writing data to file system not supported");
        }

        public string GetData(string id)
        {
            var path = FilePath(id);
            Console.WriteLine("##### " + path);
            if (!File.Exists(path)) {
                return null;
            }

            try {
                var text = File.ReadAllText(path);
                return text;
            } catch (Exception ex) {
                return ex.Message;
            }
        }
    }
}
