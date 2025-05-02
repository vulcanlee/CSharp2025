using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace csAzureBlobStorage
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 讀取環境變數內的 Azure Blob Storage 的連線字串
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            string containerName = "audio-files";     // 要上傳到的 container
            // 取得當前目錄
            string currentDirectory = Directory.GetCurrentDirectory();
            string localFilePath =Path.Combine( Directory.GetCurrentDirectory(), "250501_0814.mp3"); 
            string blobName = Path.GetFileName(localFilePath); // blob 名稱

            // --------------------------------

            // 建立 BlobServiceClient (方法 A)
            var blobServiceClient = new BlobServiceClient(connectionString);

            // 取得 container client，若 container 不存在則自動建立
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // 取得指定 blob 的 client
            var blobClient = containerClient.GetBlobClient(blobName);

            Console.WriteLine($"開始上傳 {localFilePath} → {containerClient.Uri}/{blobName} ...");

            // 以檔案串流上傳，並設定 ContentType 以利瀏覽器正確播放
            using FileStream uploadFileStream = File.OpenRead(localFilePath);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = "audio/mpeg" };

            // 如果檔案很大，可以傳入 BlobUploadOptions 並設定 TransferOptions 分段上傳
            await blobClient.UploadAsync(
                uploadFileStream,
                new BlobUploadOptions { HttpHeaders = blobHttpHeaders }
            );

            uploadFileStream.Close();
            Console.WriteLine("上傳完成！");

            var blobItemClient = containerClient.GetBlobClient(blobName);

            // 直接拿 URL
            Uri blobUri = blobItemClient.Uri;
            Console.WriteLine(blobUri.ToString());

            // 取得與顯示 Blob Storage 的音檔 SAS URI
            var sasToken = blobItemClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(5));
            Console.WriteLine($"SAS URI: {sasToken}");

        }
    }
}
