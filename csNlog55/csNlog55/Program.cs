using csNlog55.Components;
using NLog;
using NLog.Web;

namespace csNlog55;

public class Program
{
    public static void Main(string[] args)
    {
        // 早期初始化 NLog，用於記錄啟動和異常
        //var logger = LogManager.Setup()
        //    .SetupExtensions(s => s.RegisterAssembly(typeof(NLogAspNetCoreOptions).Assembly))
        //    .LoadConfigurationFromFile("nlog.config")
        //    .GetCurrentClassLogger();

        try
        {
            //LogManager.GetCurrentClassLogger().Info("Starting application...");

            var builder = WebApplication.CreateBuilder(args);

            #region NLog 相關設定
            var nlogBasePrefixPath = builder.Configuration.GetValue<string>("NLog:BasePath");
            var baseNamespace = typeof(Program).Namespace;

            string nlogBasePath = null;
            if (!string.IsNullOrWhiteSpace(nlogBasePrefixPath))
            {
                nlogBasePath = Path.Combine(nlogBasePrefixPath, baseNamespace);
                Directory.CreateDirectory(nlogBasePath);

                // 設置內部日誌記錄器
                NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Info;
                NLog.Common.InternalLogger.LogFile = Path.Combine(nlogBasePath, $"{baseNamespace}-nlog-internal.log");

                // 設置變量到當前配置
                LogManager.Configuration.Variables["BasePath"] = nlogBasePath;
                LogManager.Configuration.Variables["LogFilenamePrefix"] = $"{baseNamespace}-logfile";

                //LogManager.GetCurrentClassLogger().Info("NLog configured with BasePath: {BasePath}", nlogBasePath);
            }

            builder.Logging.ClearProviders();
            builder.Host.UseNLog();
            #endregion

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            LogManager.GetCurrentClassLogger().Info("Application built successfully");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex, "Stopped program because of an exception");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
