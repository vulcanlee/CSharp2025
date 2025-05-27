using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Drawing;
using System.IO;

namespace csDicomToImage
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 初始化 DICOM 設定
            InitializeDicom();

            string dicomFile = "image-00000.dcm";
            string pngFile = "out.png";
            string rootPath = @"C:\Vulcan\temp\series-00000";
            string dicomFilePath = Path.Combine(rootPath, dicomFile);
            string pngFilePath = Path.Combine(rootPath, pngFile);

            // 轉換單個檔案
            ConvertSingleFile(dicomFilePath, pngFilePath);

            // 批次轉換資料夾中的所有 DICOM 檔案
            // ConvertDirectory("input_folder", "output_folder");
        }

        /// <summary>
        /// 初始化 DICOM 設定，註冊 ImageSharp 影像管理器
        /// </summary>
        private static void InitializeDicom()
        {
            try
            {
                // 在應用程式啟動時初始化
                //new DicomSetupBuilder()
                //  .RegisterServices(s => s.AddFellowOakDicom()
                //  .AddTranscoderManager<FellowOakDicom.Imaging.NativeCodec.NativeTranscoderManager>())
                //  .SkipValidation()
                //  .Build();

                new DicomSetupBuilder()
                    .RegisterServices(s =>
                    s.AddFellowOakDicom()
                     .AddTranscoderManager<FellowOakDicom.Imaging.NativeCodec.NativeTranscoderManager>()
                     .AddImageManager<ImageSharpImageManager>())
              .SkipValidation()
              .Build();

                //new DicomSetupBuilder()
                //    .RegisterServices(s => s.AddFellowOakDicom()
                //    .AddImageManager<ImageSharpImageManager>())
                //    .SkipValidation()
                //    .Build();

                Console.WriteLine("DICOM 初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DICOM 初始化失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 轉換單個 DICOM 檔案為 PNG
        /// </summary>
        /// <param name="dicomPath">DICOM 檔案路徑</param>
        /// <param name="pngPath">PNG 輸出路徑</param>
        public static void ConvertSingleFile(string dicomPath, string pngPath)
        {
            try
            {
                Console.WriteLine($"開始轉換: {dicomPath}");

                // 開啟 DICOM 檔案
                var dicomFile = DicomFile.Open(dicomPath);
                var dicomImage = new DicomImage(dicomFile.Dataset);

                // 渲染影像
                //var foo = dicomImage.RenderImage().As<Bitmap>();
                var renderedImage = dicomImage.RenderImage();

                // 將 renderedImage 轉換為 ImageSharp Image 並儲存為 PNG
                var sharpImage = renderedImage.AsSharpImage();

                // 儲存為 PNG
                using (var fileStream = new FileStream(pngPath, FileMode.Create))
                {
                    sharpImage.Save(fileStream, new PngEncoder());
                }

                Console.WriteLine($"轉換完成: {pngPath}");

                // 顯示影像資訊
                //DisplayImageInfo(dicomFile.Dataset, sharpImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"轉換失敗: {ex.Message}");
                throw;
            }
        }
    }
}
