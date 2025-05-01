using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace csSpeechToTextBatch
{
    // 定義 JSON 結構對應的 POCO
    class TranscriptionJson
    {
        [JsonProperty("combinedRecognizedPhrases")]
        public CombinedPhrase[] CombinedRecognizedPhrases { get; set; }
    }
    class CombinedPhrase
    {
        [JsonProperty("display")]
        public string Display { get; set; }
    }
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Speech 服務金鑰與區域
            string SubscriptionKey = Environment.GetEnvironmentVariable("AzureSpeechServiceSubscriptionKey");
            string ServiceRegion = Environment.GetEnvironmentVariable("AzureSpeechServiceRegion");

            // 上傳到 Blob Storage 的音檔 SAS URI
            string AudioFileSasUri = "https://blogstoragekh.blob.core.windows.net/audio-files/250501_0814.mp3?sv=2025-05-05&se=2025-05-01T10%3A53%3A53Z&sr=b&sp=r&sig=HVG%2Bs3hxD5cmv%2FrVOs5HZbekqmIBJujOGJWnsRLTjUQ%3D";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

            // 1. 建立轉錄工作
            var createUrl = $"https://{ServiceRegion}.api.cognitive.microsoft.com/speechtotext/v3.2/transcriptions";
            var createBody = new
            {
                contentUrls = new[] { AudioFileSasUri },
                locale = "zh-TW",
                displayName = "My Batch Transcription",
                properties = new
                {
                    diarizationEnabled = false,
                    wordLevelTimestampsEnabled = true,
                    punctuationMode = "DictatedAndAutomatic"
                }
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(createBody));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var createResponse = await client.PostAsync(createUrl, jsonContent);
            createResponse.EnsureSuccessStatusCode();

            var createResult = await createResponse.Content.ReadAsStringAsync();
            Console.WriteLine("已建立批次轉錄工作：");
            Console.WriteLine(createResult);

            // 解析 self URL
            dynamic createJson = JsonConvert.DeserializeObject(createResult);
            string transcriptionUrl = createJson.self;

            // 2. 輪詢狀態
            Console.WriteLine("開始輪詢轉錄狀態…");
            TimeSpan elapsedTime;
            DateTime startTime = DateTime.Now;
            while (true)
            {
                elapsedTime = DateTime.Now - startTime;
                // 顯示已經花費時間 小時:分鐘:秒
                Console.WriteLine($"已經花費時間：{elapsedTime.Hours:D2}:{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}");

                var statusResponse = await client.GetAsync(transcriptionUrl);
                statusResponse.EnsureSuccessStatusCode();

                var statusJson = await statusResponse.Content.ReadAsStringAsync();
                dynamic statusObj = JsonConvert.DeserializeObject(statusJson);
                string status = statusObj.status;
                Console.WriteLine($"目前狀態：{status}");

                if (status == "Succeeded")
                {
                    // 3. 取得並下載轉錄結果
                    string filesUrl = statusObj.links.files;
                    var filesResponse = await client.GetAsync(filesUrl);
                    filesResponse.EnsureSuccessStatusCode();

                    var filesJson = await filesResponse.Content.ReadAsStringAsync();
                    dynamic filesObj = JsonConvert.DeserializeObject(filesJson);

                    foreach (var file in filesObj.values)
                    {
                        if ((string)file.kind == "Transcription")
                        {
                            var fileUrl = (string)file.links.contentUrl;
                            var transcriptionResult = await client.GetStringAsync(fileUrl);
                            //Console.WriteLine("---- 轉錄結果 ----");
                            //Console.WriteLine(transcriptionResult);

                            // 取得最終錄音文字
                            // 反序列化
                            var resultObj = JsonConvert.DeserializeObject<TranscriptionJson>(transcriptionResult);

                            // 串接所有 display 文字，並印出完整內容
                            string fullText = string.Join(" ",
                                resultObj.CombinedRecognizedPhrases
                                         .Select(p => p.Display?.Trim())
                                         .Where(s => !string.IsNullOrEmpty(s))
                            );
                            Console.WriteLine("---- 完整轉錄文字 ----");
                            Console.WriteLine(fullText);
                        }
                    }
                    break;
                }
                else if (status == "Failed")
                {
                    Console.WriteLine("轉錄失敗");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
