using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace csDiagnosticReport;

internal class Program
{
    private const string FhirBaseUrl = "http://10.1.1.113:8080/fhir";

    // 自家 CodeSystem（若你未導入 LOINC/其他標準碼，先用自家碼很常見）
    private const string CsBodyComp = "https://example.org/fhir/CodeSystem/body-composition";
    private const string CsObsCategory = "http://terminology.hl7.org/CodeSystem/observation-category";
    private const string CsV2_0074 = "http://terminology.hl7.org/CodeSystem/v2-0074";
    private const string Ucum = "http://unitsofmeasure.org";
    private const string patientId = "test-patient-001"; // 固定的 Patient ID
    private const string AIResultImageUrl = "http://localhost/result/2311"; // 固定的 Patient ID
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = cts.Token;

        var client = CreateFhirClient(FhirBaseUrl);

        // 1) 隨機取得一位既有 Patient
        //var patient = await GetSomeonePatientAsync(client, ct);
        var patient = await CreateTestPatientAsync(client, ct);
        Console.WriteLine($"Selected Patient: {patient.Id}");

        // 2) 準備六個指標數值（示範：你可改成實際推論結果）
        // SMI: cm2/m2, SMD: HU, 其餘: cm2
        var effectiveTime = DateTimeOffset.Now;

        var smi = 29.42m;
        var smd = 39.50m;
        var imat = 11.69m;
        var lama = 20.93m;
        var nama = 69.16m;
        var myosteatosis = 32.62m;

        // 3) 建立 Observation + DiagnosticReport，並 Transaction 寫入
        var transaction = BuildTransactionBundle(
            patientId: patient.Id,
            effectiveTime: effectiveTime,
            imageUrl: AIResultImageUrl,
            smi: smi,
            smd: smd,
            imat: imat,
            lama: lama,
            nama: nama,
            myosteatosis: myosteatosis
        );

        var response = await client.TransactionAsync(transaction, ct);

        // 4) 從回應 Bundle 取出 server 指派的資源 id（如果 server 有回傳）
        // 有些 server 會把 location 放在 entry.response.location
        Console.WriteLine("Transaction completed.");
        foreach (var entry in response.Entry)
        {
            var loc = entry.Response?.Location ?? "(no location)";
            Console.WriteLine($"- {entry.Resource?.TypeName ?? "(no resource)"} => {loc}");
        }

        await DeletePatientAndRelatedResourcesAsync(client, patient.Id, ct);
    }

    /// <summary>
    /// 刪除指定 Patient 及其所有相關資源
    /// - 搜尋並刪除相關的 Observation
    /// - 搜尋並刪除相關的 DiagnosticReport
    /// - 最後刪除 Patient 本身
    /// </summary>
    private static async System.Threading.Tasks.Task DeletePatientAndRelatedResourcesAsync(
        FhirClient client,
        string patientId,
        CancellationToken ct)
    {
        Console.WriteLine($"開始刪除 Patient ID: {patientId} 及其相關資源...");

        var deleteEntries = new List<Bundle.EntryComponent>();

        try
        {
            // 1. 搜尋相關的 Observation
            var obsBundle = await client.SearchAsync<Observation>(
                new[] { $"subject=Patient/{patientId}" },
                ct: ct);

            if (obsBundle?.Entry != null)
            {
                foreach (var entry in obsBundle.Entry)
                {
                    if (entry.Resource is Observation obs && !string.IsNullOrEmpty(obs.Id))
                    {
                        deleteEntries.Add(new Bundle.EntryComponent
                        {
                            Request = new Bundle.RequestComponent
                            {
                                Method = Bundle.HTTPVerb.DELETE,
                                Url = $"Observation/{obs.Id}"
                            }
                        });
                        Console.WriteLine($"- 標記刪除 Observation: {obs.Id}");
                    }
                }
            }

            // 2. 搜尋相關的 DiagnosticReport
            var drBundle = await client.SearchAsync<DiagnosticReport>(
                new[] { $"subject=Patient/{patientId}" },
                ct: ct);

            if (drBundle?.Entry != null)
            {
                foreach (var entry in drBundle.Entry)
                {
                    if (entry.Resource is DiagnosticReport dr && !string.IsNullOrEmpty(dr.Id))
                    {
                        deleteEntries.Add(new Bundle.EntryComponent
                        {
                            Request = new Bundle.RequestComponent
                            {
                                Method = Bundle.HTTPVerb.DELETE,
                                Url = $"DiagnosticReport/{dr.Id}"
                            }
                        });
                        Console.WriteLine($"- 標記刪除 DiagnosticReport: {dr.Id}");
                    }
                }
            }

            // 3. 最後刪除 Patient
            deleteEntries.Add(new Bundle.EntryComponent
            {
                Request = new Bundle.RequestComponent
                {
                    Method = Bundle.HTTPVerb.DELETE,
                    Url = $"Patient/{patientId}"
                }
            });
            Console.WriteLine($"- 標記刪除 Patient: {patientId}");

            // 4. 使用 Transaction Bundle 執行批次刪除
            if (deleteEntries.Count > 0)
            {
                var deleteBundle = new Bundle
                {
                    Type = Bundle.BundleType.Transaction,
                    Entry = deleteEntries
                };

                var response = await client.TransactionAsync(deleteBundle, ct);
                Console.WriteLine($"成功刪除 {deleteEntries.Count} 筆資源");

                // 顯示刪除結果
                foreach (var entry in response.Entry)
                {
                    var status = entry.Response?.Status ?? "unknown";
                    var location = entry.Response?.Location ?? "unknown";
                    Console.WriteLine($"  - {location}: {status}");
                }
            }
            else
            {
                Console.WriteLine("未找到需要刪除的資源");
            }
        }
        catch (FhirOperationException ex)
        {
            Console.Error.WriteLine($"刪除失敗: {ex.Message}");
            Console.Error.WriteLine($"Status: {ex.Status}");
            throw;
        }
    }

    private static FhirClient CreateFhirClient(string baseUrl)
    {
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            VerifyFhirVersion = true,
            ReturnPreference = ReturnPreference.Representation
        };

        return new FhirClient(baseUrl, settings);
    }


    private static async Task<Patient> CreateTestPatientAsync(FhirClient client, CancellationToken ct)
    {
        try
        {
            var patient = new Patient
            {
                Id = patientId,
                Active = true,
                Name =
                {
                    new HumanName
                    {
                        Use = HumanName.NameUse.Official,
                        Family = "測試",
                        Given = new[] { "病患", "範例" }
                    }
                },
                Gender = AdministrativeGender.Male,
                BirthDate = "1980-01-01",
                Telecom =
                {
                    new ContactPoint
                    {
                        System = ContactPoint.ContactPointSystem.Phone,
                        Value = "0912-345-678",
                        Use = ContactPoint.ContactPointUse.Mobile
                    }
                }
            };

            // 使用 UpdateAsync (HTTP PUT)。
            // 如果資源被刪除 (Gone)，這會直接產生一個新版本將其復活。
            // 如果資源不存在 (NotFound)，這會直接建立它。
            var createdPatient = await client.UpdateAsync(patient);
            Console.WriteLine($"已建立病患: {createdPatient.Id}");
            return createdPatient;
        }
        catch (FhirOperationException ex)
        {
            Console.WriteLine($"無法建立/更新病患: {ex.Message}");
            throw;
        }

        // 不應該到達這裡
        throw new InvalidOperationException("無法建立或取得 Patient");
    }
    
    /// <summary>
         /// 取得「隨機」Patient：
         /// - 先抓 50 筆（Patient?_count=50）
         /// - 從這一批隨機挑一筆
         ///
         /// 注意：FHIR 標準沒有真正 random/offset 的機制；
         /// 這個方法在大多數情境已足夠做 demo / sample。
         /// </summary>
    private static async Task<Patient> GetSomeonePatientAsync(FhirClient client, CancellationToken ct)
    {
        // 抓一批病人
        var bundle = await client.SearchAsync<Patient>(new[] { "_count=50" }, ct: ct);

        if (bundle.Entry == null || bundle.Entry.Count == 0)
            throw new InvalidOperationException("No Patient found in FHIR server.");

        var patients = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Patient>()
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .ToList();

        if (patients.Count == 0)
            throw new InvalidOperationException("No Patient with Id found in search result.");

        var picked = patients[0];
        Console.WriteLine($"發現到病患 {picked.Name.FirstOrDefault()} : {picked.Id}");
        return picked;
    }

    /// <summary>
    /// 建立 Transaction Bundle：
    /// - Observation：1筆，含6個 component 指標
    /// - DiagnosticReport：result 指向 Observation，presentedForm 包含圖片 URL
    /// - 兩者 subject 都 reference 到既有 Patient/{id}
    /// </summary>
    private static Bundle BuildTransactionBundle(
        string patientId,
        DateTimeOffset effectiveTime,
        string imageUrl,
        decimal smi,
        decimal smd,
        decimal imat,
        decimal lama,
        decimal nama,
        decimal myosteatosis)
    {
        var patientRef = new ResourceReference($"Patient/{patientId}");

        // 用 urn:uuid 讓 transaction 內互相 reference（乾淨、一次送）
        var obsFullUrl = $"urn:uuid:{Guid.NewGuid()}";
        var drFullUrl = $"urn:uuid:{Guid.NewGuid()}";

        var obs = BuildObservation(patientRef, effectiveTime, smi, smd, imat, lama, nama, myosteatosis);
        var dr = BuildDiagnosticReport(patientRef, effectiveTime, obsFullUrl, imageUrl);

        return new Bundle
        {
            Type = Bundle.BundleType.Transaction,
            Entry =
            {
                NewPostEntry(obsFullUrl, "Observation", obs),
                NewPostEntry(drFullUrl, "DiagnosticReport", dr)
            }
        };
    }

    private static Bundle.EntryComponent NewPostEntry(string fullUrl, string url, Resource resource) =>
        new Bundle.EntryComponent
        {
            FullUrl = fullUrl,
            Resource = resource,
            Request = new Bundle.RequestComponent
            {
                Method = Bundle.HTTPVerb.POST,
                Url = url
            }
        };

    private static Observation BuildObservation(
        ResourceReference patientRef,
        DateTimeOffset effectiveTime,
        decimal smi,
        decimal smd,
        decimal imat,
        decimal lama,
        decimal nama,
        decimal myosteatosis)
    {
        var obs = new Observation
        {
            Status = ObservationStatus.Final,
            Subject = patientRef,
            Effective = new FhirDateTime(effectiveTime),
            Category =
            {
                new CodeableConcept(CsObsCategory, "imaging", "Imaging", null)
            },
            Code = new CodeableConcept(CsBodyComp, "body-composition-summary", "Body composition AI summary", null),
        };

        // 6 個 component：你指定的做法
        obs.Component.Add(Component("SMI", "骨骼肌指標 (SMI)", Quantity(smi, "cm2/m2", Ucum, "cm2/m2")));
        obs.Component.Add(Component("SMD", "骨骼肌密度 (SMD)", new Quantity { Value = smd, Unit = "HU" }));
        obs.Component.Add(Component("IMAT", "肌間/肌內脂肪組織 (IMAT)", Quantity(imat, "cm2", Ucum, "cm2")));
        obs.Component.Add(Component("LAMA", "低密度肌肉區域 (LAMA)", Quantity(lama, "cm2", Ucum, "cm2")));
        obs.Component.Add(Component("NAMA", "正常密度肌肉區域 (NAMA)", Quantity(nama, "cm2", Ucum, "cm2")));
        obs.Component.Add(Component("MYOSTEATOSIS", "肌肉脂肪變性 (Myosteatosis)", Quantity(myosteatosis, "cm2", Ucum, "cm2")));

        return obs;
    }

    private static DiagnosticReport BuildDiagnosticReport(
        ResourceReference patientRef,
        DateTimeOffset effectiveTime,
        string observationFullUrl,
        string imageUrl)
    {
        var diagnosticReport = new DiagnosticReport
        {
            Status = DiagnosticReport.DiagnosticReportStatus.Final,
            Subject = patientRef,
            Effective = new FhirDateTime(effectiveTime),
            Category =
            {
                new CodeableConcept(CsV2_0074, "RAD", "Radiology", null)
            },
            Code = new CodeableConcept(CsBodyComp, "bodycomp-ai-report", "Body composition AI report", null),
            Result =
            {
                // 指向 transaction 內的 Observation
                new ResourceReference(observationFullUrl)
            }
        };

        // 使用 URL 參照外部圖片，而非嵌入圖片資料
        diagnosticReport.PresentedForm.Add(new Attachment
        {
            ContentType = "image/png",
            Title = "Segmentation overlay",
            Url = imageUrl
        });

        return diagnosticReport;
    }

    private static Observation.ComponentComponent Component(string code, string text, Quantity qty) =>
        new Observation.ComponentComponent
        {
            Code = new CodeableConcept(CsBodyComp, code, text, null),
            Value = qty
        };

    private static Quantity Quantity(decimal value, string unit, string system, string code) =>
        new Quantity
        {
            Value = value,
            Unit = unit,
            System = system,
            Code = code
        };
}
