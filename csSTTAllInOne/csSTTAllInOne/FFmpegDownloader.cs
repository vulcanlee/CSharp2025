using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace csSTTAllInOne
{
    public class FFmpegDownloader
    {
        public async Task DownloadFFmpegAsync()
        {
            var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");
            var ffmpegDownloadPath = Path.Combine(ffmpegPath, "ffmpeg-master-latest-win64-gpl",
                "bin");
            var ffmpegDownloadRootPath = Path.Combine(ffmpegPath, "ffmpeg-master-latest-win64-gpl");
            var ffmpegDownloadFile = Path.Combine(ffmpegDownloadPath, "ffmpeg.exe");
            var ffmpegFile = Path.Combine(ffmpegPath, "ffmpeg.exe");

            if (!File.Exists(ffmpegFile))
            {
                if (Directory.Exists(ffmpegDownloadRootPath))
                {
                    Directory.Delete(ffmpegDownloadRootPath, true);
                }

                using var httpClient = new HttpClient();
                string downloadUrl;
                string fileName;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    downloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
                    fileName = "ffmpeg-win64.zip";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    downloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz";
                    fileName = "ffmpeg-linux64.tar.xz";
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported platform");
                }

                var zipPath = Path.Combine(ffmpegPath, fileName);

                // 下載檔案
                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                await using var fileStream = File.Create(zipPath);
                await response.Content.CopyToAsync(fileStream);
                fileStream.Close();

                // 解壓縮
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, ffmpegPath);
                }

                // 清理下載檔案
                File.Delete(zipPath);

                File.Copy(ffmpegDownloadFile, ffmpegFile, true);
                Directory.Delete(ffmpegDownloadRootPath, true);
            }

            GlobalFFOptions.Configure(new FFOptions
            {
                BinaryFolder = ffmpegPath,
                TemporaryFilesFolder = Path.GetTempPath()
            });
        }
    }
}
