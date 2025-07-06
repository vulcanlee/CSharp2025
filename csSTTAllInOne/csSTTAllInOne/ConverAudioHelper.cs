using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csSTTAllInOne
{
    public class ConverAudioHelper
    {
        List<string> AcceptConvertFiles = new()
        {
            // 聲音格式
            ".mp3", ".aac", ".ogg", ".wma", ".m4a", ".flac", ".ape", ".aiff",
            
            // 影片格式
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".3gp",
            ".m4v", ".ts", ".mts", ".m2ts"
        }; 

        public async Task ConvertToWav()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            var allFiles = Directory.GetFiles(currentDirectory, "*.*").ToList();
            var audioSearchedFiles = allFiles
                .Where(file => AcceptConvertFiles.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            foreach (var videoAudioFile in audioSearchedFiles)
            {
                string videoAudioFileWithoutExtension = Path.GetFileNameWithoutExtension(videoAudioFile);
                Console.WriteLine($"發現音訊/影片文件: {videoAudioFile}");


                // 初始化 FFMpegCore，指定 ffmpeg / ffprobe 執行檔位置
                // 請修改為你本機或部署環境中 ffmpeg 的安裝路徑
                //string ffmpegRoot = @"C:\tools\ffmpeg\bin";
                //GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegRoot);

                var inputPath = videoAudioFile;

                // 若未指定輸出檔案，預設將副檔名改為 .wav
                var outputPath = $"{videoAudioFileWithoutExtension}.wav";

                try
                {
                    Console.WriteLine($"正在將 '{inputPath}' 轉換為 '{outputPath}'...");

                    // 執行轉檔指令，並指定音訊編碼、取樣率與聲道數
                    await FFMpegArguments
                        .FromFileInput(inputPath)
                        .OutputToFile(outputPath, overwrite: true, options => options
                            //.WithAudioCodec(AudioCodec.pcm_s16le)
                            .WithAudioCodec(FFMpeg.GetCodec("pcm_s16le"))
                            //.WithAudioSamplingRate(44100)
                            .WithAudioSamplingRate(16000)
                            .WithCustomArgument("-ac 1")      // 單聲道
                            )
                        .ProcessAsynchronously();

                    Console.WriteLine("轉換完成");

                    File.Delete(inputPath); // 刪除原始檔案
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"轉換失敗: {ex.Message}");
                }
            }
        }

        public async Task ConvertToMp3()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            var allFiles = Directory.GetFiles(currentDirectory, "*.*").ToList();
            var audioSearchedFiles = allFiles
                .Where(file => AcceptConvertFiles.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            foreach (var videoAudioFile in audioSearchedFiles)
            {
                if(Path.GetExtension(videoAudioFile).ToLowerInvariant() == ".mp3")
                {
                    continue; // 如果已經是 MP3 格式，則跳過
                }

                string videoAudioFileWithoutExtension = Path.GetFileNameWithoutExtension(videoAudioFile);
                Console.WriteLine($"發現音訊/影片文件: {videoAudioFile}");


                // 初始化 FFMpegCore，指定 ffmpeg / ffprobe 執行檔位置
                // 請修改為你本機或部署環境中 ffmpeg 的安裝路徑
                //string ffmpegRoot = @"C:\tools\ffmpeg\bin";
                //GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegRoot);

                var inputPath = videoAudioFile;

                // 若未指定輸出檔案，預設將副檔名改為 .mp3
                var outputPath = $"{videoAudioFileWithoutExtension}.mp3";

                try
                {
                    Console.WriteLine($"正在將 '{inputPath}' 轉換為 '{outputPath}'...");

                    // 執行轉檔指令，並指定音訊編碼、取樣率與聲道數
                    await FFMpegArguments
                        .FromFileInput(inputPath)
                        .OutputToFile(outputPath, overwrite: true, options => options
                            //.WithAudioCodec(AudioCodec.mp3)
                            .WithAudioCodec(FFMpeg.GetCodec("mp3"))
                            .WithAudioSamplingRate(44100)
                            //.WithAudioSamplingRate(16000)
                            .WithCustomArgument("-ac 1")      // 單聲道
                            )
                        .ProcessAsynchronously();

                    Console.WriteLine("轉換完成");

                    File.Delete(inputPath); // 刪除原始檔案
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"轉換失敗: {ex.Message}");
                }
            }
        }
    }
}
