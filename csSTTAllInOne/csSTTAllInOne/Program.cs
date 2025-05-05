using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace csSTTAllInOne
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 建立 ServiceCollection 並註冊服務
            var serviceCollection = new ServiceCollection();

            // 註冊 Logging
            serviceCollection.AddLogging(builder =>
            {
                builder
                    .AddConsole() // 將日誌輸出到主控台
                    .SetMinimumLevel(LogLevel.Information); // 設定最低日誌層級
            });

            // 註冊其他服務
            serviceCollection.AddSingleton<SpeechToTextService>();




            // 建立 ServiceProvider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // 取得 ILogger 實例
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("應用程式啟動中...");

            // 使用其他服務
            var speechToTextService = serviceProvider.GetRequiredService<SpeechToTextService>();

            string filename = Path.Combine(Directory.GetCurrentDirectory(), "250501_0814.mp3");
            string textScript = await speechToTextService.ProcessAsync(filename);
            logger.LogInformation("語音文稿解析完成 ： {0}", textScript);
        }
    }
}
