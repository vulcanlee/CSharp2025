
using FellowOakDicom;

namespace csDicomGetSetTagValue
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string dicomFile = "image-00000.dcm";
            string dicomUpdateFile = "image-00000Update.dcm";
            string rootPath = Directory.GetCurrentDirectory();
            string dicomFilePath = Path.Combine(rootPath, dicomFile);
            string dicomUpdateFilePath = Path.Combine(rootPath,  dicomUpdateFile);

            // 轉換單個檔案
            GetOrSetDicomTag(dicomFilePath, dicomUpdateFilePath);
        }

        private static void GetOrSetDicomTag(string dicomPath, string outputDicomPath)
        {
            try
            {
                // 開啟 DICOM 檔案
                var dicomFile = DicomFile.Open(dicomPath);

                DicomTag patientIdTag = QueryTag(dicomFile);

                SetTag(dicomFile, DicomTag.PatientID);

                DicomTag patientIdTagAgain = QueryTag(dicomFile);

                // 儲存修改後的 DICOM 檔案
                dicomFile.Save(outputDicomPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"轉換失敗: {ex.Message}");
                throw;
            }
        }

        private static void SetTag(DicomFile dicomFile, DicomTag patientIdTag)
        {
            // 針對 Patient ID 標籤，並且設定為 ABC123
            dicomFile.Dataset.AddOrUpdate(patientIdTag, Guid.NewGuid().ToString());
        }

        private static DicomTag QueryTag(DicomFile dicomFile)
        {
            // 取得 Patient ID 標籤
            var patientIdTag = DicomTag.PatientID;
            if (dicomFile.Dataset.Contains(patientIdTag))
            {
                var patientId = dicomFile.Dataset.GetString(patientIdTag);
                Console.WriteLine($"原始 Patient ID: {patientId}");
            }
            else
            {
                Console.WriteLine("Patient ID 標籤不存在。");
            }

            return patientIdTag;
        }
    }
}
