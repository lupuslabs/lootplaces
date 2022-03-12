using Microsoft.AspNetCore.HttpOverrides;
using n3q.FrameworkTools;

namespace LootPlacesWeb
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
            builder.Services.AddControllers();
            builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

            builder.Services.AddSingleton(new MyApp { });

            var app = builder.Build();

            app.Services.GetRequiredService<MyApp>().Log = new MicrosoftLoggingCallbackLogger(app.Services.GetService<ILogger<LootPlaces>>());

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });

            app.Run();
        }
    }

    public class LootPlaces { }
}
