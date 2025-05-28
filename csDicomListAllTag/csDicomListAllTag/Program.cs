using FellowOakDicom;
using FellowOakDicom.IO.Buffer;

namespace csDicomListAllTag
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string dicomFile = "image-00000.dcm";
            string rootPath = Directory.GetCurrentDirectory();
            string dicomFilePath = Path.Combine(rootPath, dicomFile);

            // 轉換單個檔案
            GetAllTags(dicomFilePath);
        }

        public static void GetAllTags(string dicomPath)
        {
            try
            {
                // 開啟 DICOM 檔案
                var dicomFile = DicomFile.Open(dicomPath);
                
                // 創建自定義訪問者以列印所有標籤
                var visitor = new DicomDatasetVisitor();
                var walker = new DicomDatasetWalker(dicomFile.Dataset);
                walker.Walk(visitor);
                
                Console.WriteLine($"DICOM 檔案 {Path.GetFileName(dicomPath)} 總共含有 {visitor.TagCount} 個標籤");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"轉換失敗: {ex.Message}");
                throw;
            }
        }
    }

    // 自定義訪問者類別，用於輸出所有 DICOM 標籤
    public class DicomDatasetVisitor : IDicomDatasetWalker
    {
        public int TagCount { get; private set; } = 0;

        public void OnBeginWalk()
        {
            Console.WriteLine("開始遍歷 DICOM 標籤...");
        }

        public void OnEndWalk()
        {
            Console.WriteLine("DICOM 標籤遍歷完成");
        }

        public bool OnElement(DicomElement element)
        {
            TagCount++;
            Console.WriteLine($"標籤: {element.Tag} ({element.Tag.DictionaryEntry?.Name ?? "Unknown"}) - 值: {GetElementValueAsString(element)}");
            return true;
        }

        public Task<bool> OnElementAsync(DicomElement element)
        {
            return Task.FromResult(OnElement(element));
        }

        public bool OnBeginSequence(DicomSequence sequence)
        {
            TagCount++;
            Console.WriteLine($"開始序列: {sequence.Tag} ({sequence.Tag.DictionaryEntry?.Name ?? "Unknown"})");
            return true;
        }

        public bool OnBeginSequenceItem(DicomDataset dataset)
        {
            Console.WriteLine("  序列項目開始 >>>");
            return true;
        }

        public bool OnEndSequenceItem()
        {
            Console.WriteLine("  <<< 序列項目結束");
            return true;
        }

        public bool OnEndSequence()
        {
            Console.WriteLine("序列結束");
            return true;
        }

        public bool OnBeginFragment(DicomFragmentSequence fragment)
        {
            TagCount++;
            Console.WriteLine($"開始片段: {fragment.Tag} ({fragment.Tag.DictionaryEntry?.Name ?? "Unknown"})");
            return true;
        }

        public bool OnFragmentItem(IByteBuffer item)
        {
            Console.WriteLine($"  片段項目: {item.Size} 位元組");
            return true;
        }

        public Task<bool> OnFragmentItemAsync(IByteBuffer item)
        {
            return Task.FromResult(OnFragmentItem(item));
        }

        public bool OnEndFragment()
        {
            Console.WriteLine("片段結束");
            return true;
        }

        private string GetElementValueAsString(DicomElement element)
        {
            try
            {
                return element.ToString();
            }
            catch
            {
                return "[無法顯示的值]";
            }
        }
    }
}
