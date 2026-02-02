using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace csObservation;

internal class Program
{
    static void Main(string[] args)
    {
        // ======== 你需要改的設定 ========
        var fhirBaseUrl = "https://hapi.fhir.tw/fhir";  // 例如 HAPI FHIR: http://10.1.1.113:8080/fhir
        var patientId = "1084";                        // 指定 Patient/{id}
        var startDate = new DateTimeOffset(2014, 01, 01, 0, 0, 0, TimeSpan.FromHours(8));
        var endDate = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.FromHours(8));

        // 建議：正式環境要處理 TLS/驗證/OAuth2，這裡先示範基本讀取
        var clientSettings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
            Timeout = 60_000,
            VerifyFhirVersion = false // 公開 HAPI 伺服器，版本檢查失敗時避免直接丟例外
        };

        FhirClient client;
        try
        {
            // 確保 Base Url 合法且有結尾斜線
            var baseUri = new Uri(fhirBaseUrl.TrimEnd('/') + "/");
            client = new FhirClient(baseUri, clientSettings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"建立 FhirClient 失敗，請檢查 fhirBaseUrl 設定：{ex.Message}");
            return;
        }

        // ======== 查詢 Observation ========
        // 常見條件：
        // - subject=Patient/{id}
        // - date=ge... & date=le...（effectiveDateTime / effectivePeriod 會吃 date 參數）
        // - category=laboratory 或 vital-signs（用 category 過濾）
        // - code=LOINC（指定某些檢驗/生命徵象）
        // - _count、_sort、_summary、_include 等
        var searchParams = new SearchParams()
            .Where($"subject=Patient/{patientId}")
            .Where($"date=ge{startDate:yyyy-MM-dd}")
            .Where($"date=le{endDate:yyyy-MM-dd}")
            .OrderBy("-date")      // 「-欄位」代表 desc
            .LimitTo(100);         // 舊版/新版都有的介面，比 Count(100) 相容性高

        // 你可以視需求 include 參考資源（伺服器要支援）
        // Observation 常見 include:
        // - subject (Patient)  encounter  performer  specimen  device
        // 注意：include 太多可能很慢，真正在產品通常會分開查或做快取
        // 新版 API 的 Include 集合需要指定 IncludeModifier，這裡使用 None
        searchParams.Include.Add(("Observation:subject", IncludeModifier.None));
        searchParams.Include.Add(("Observation:encounter", IncludeModifier.None));
        searchParams.Include.Add(("Observation:performer", IncludeModifier.None));
        searchParams.Include.Add(("Observation:specimen", IncludeModifier.None));
        searchParams.Include.Add(("Observation:device", IncludeModifier.None));

        Bundle bundle = client.Search<Observation>(searchParams);

        Console.WriteLine($"FHIR Base: {fhirBaseUrl}");
        Console.WriteLine($"Patient: Patient/{patientId}");
        Console.WriteLine($"Range: {startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}");
        Console.WriteLine(new string('-', 80));

        int row = 0;
        foreach (var entry in bundle.Entry.Where(e => e.Resource is Observation))
        {
            var obs = (Observation)entry.Resource;
            row++;

            Console.WriteLine($"[{row}] Observation/{obs.Id}");
            Console.WriteLine($"Status: {obs.Status}  |  Category: {FormatCodeableConcepts(obs.Category)}");
            Console.WriteLine($"Code: {FormatCodeableConcept(obs.Code)}");
            Console.WriteLine($"Effective: {FormatEffective(obs.Effective)}");

            // 值與單位
            Console.WriteLine($"Value: {FormatValue(obs.Value)}");
            if (obs.ReferenceRange?.Any() == true)
            {
                Console.WriteLine($"ReferenceRange: {FormatReferenceRanges(obs.ReferenceRange)}");
            }

            // 這筆 Observation 參考到哪些 Resource（你最想看的重點）
            Console.WriteLine($"Subject: {FormatRef(obs.Subject)}");                 // 幾乎一定是 Patient（或 Group/Device）
            Console.WriteLine($"Encounter: {FormatRef(obs.Encounter)}");             // 常用：連回就醫事件
            Console.WriteLine($"Performer: {FormatRefs(obs.Performer)}");            // 可能是 Practitioner / PractitionerRole / Organization / CareTeam / Patient / Device
            Console.WriteLine($"Specimen: {FormatRef(obs.Specimen)}");               // 檢體 (laboratory 常見)
            Console.WriteLine($"Device: {FormatRef(obs.Device)}");                   // 量測儀器或系統
            Console.WriteLine($"BasedOn: {FormatRefs(obs.BasedOn)}");                // ServiceRequest / MedicationRequest / CarePlan ...
            Console.WriteLine($"PartOf: {FormatRefs(obs.PartOf)}");                  // Procedure / MedicationAdministration / ImagingStudy ...
            Console.WriteLine($"HasMember: {FormatRefs(obs.HasMember)}");            // Panel/Group
            Console.WriteLine($"DerivedFrom: {FormatRefs(obs.DerivedFrom)}");        // 由其他 Observation/DocumentReference/ImagingStudy 派生

            // Coding system / 資料分類推論（示範性提示）
            Console.WriteLine($"CodingSystems(code): {ListCodingSystems(obs.Code)}");
            var categorySystemsStr = obs.Category == null
                ? string.Empty
                : string.Join(", ", obs.Category.Select(ListCodingSystems));
            Console.WriteLine($"CodingSystems(category): {categorySystemsStr}");
            Console.WriteLine($"LikelyDomainHint: {InferDomainHint(obs)}");

            Console.WriteLine(new string('-', 80));
        }

        // 若有下一頁
        while (bundle.NextLink != null)
        {
            bundle = client.Continue(bundle, PageDirection.Next);
            foreach (var entry in bundle.Entry.Where(e => e.Resource is Observation))
            {
                // 你可以把上面顯示邏輯抽成 method，這裡省略以免太長
            }
        }
    }

    // ======== 格式化工具 ========

    static string FormatEffective(DataType? effective)
    {
        return effective switch
        {
            FhirDateTime dt => dt.Value,
            Period p => $"{p.Start} ~ {p.End}",
            Timing t => "Timing(...)",
            Instant i => i.Value?.ToString("O") ?? "",
            _ => effective?.TypeName ?? ""
        };
    }

    static string FormatValue(DataType? value)
    {
        if (value == null) return "(no value)";
        return value switch
        {
            Quantity q => $"{q.Value} {q.Unit} (system={q.System}, code={q.Code})",
            CodeableConcept cc => FormatCodeableConcept(cc),
            FhirString s => s.Value,
            FhirBoolean b => b.Value?.ToString() ?? "",
            Integer i => i.Value?.ToString() ?? "",
            Hl7.Fhir.Model.Range r => $"{FormatValue(r.Low)} ~ {FormatValue(r.High)}",
            Ratio ratio => $"{FormatValue(ratio.Numerator)} / {FormatValue(ratio.Denominator)}",
            SampledData sd => $"SampledData(points={sd.Data?.Split(' ').Length ?? 0}, unit={sd.Origin?.Unit})",
            Attachment a => $"Attachment(contentType={a.ContentType}, url={a.Url})",
            _ => $"{value.TypeName}"
        };
    }

    static string FormatReferenceRanges(List<Observation.ReferenceRangeComponent> ranges)
    {
        return string.Join(" | ", ranges.Select(r =>
        {
            var low = r.Low != null ? $"{r.Low.Value} {r.Low.Unit}" : "";
            var high = r.High != null ? $"{r.High.Value} {r.High.Unit}" : "";
            var text = r.Text ?? "";
            var type = r.Type != null ? FormatCodeableConcept(r.Type) : "";
            return $"[{low}~{high}] {text} {type}".Trim();
        }));
    }

    static string FormatCodeableConcept(CodeableConcept? cc)
    {
        if (cc == null) return "(none)";
        var text = !string.IsNullOrWhiteSpace(cc.Text) ? cc.Text : "";
        var codings = cc.Coding?.Select(c => $"{c.System}|{c.Code}{(string.IsNullOrWhiteSpace(c.Display) ? "" : $" ({c.Display})")}") ?? Enumerable.Empty<string>();
        return $"{text}  =>  {string.Join(", ", codings)}".Trim();
    }

    static string FormatCodeableConcepts(IEnumerable<CodeableConcept>? ccs)
        => ccs == null ? "(none)" : string.Join(" ; ", ccs.Select(FormatCodeableConcept));

    static string FormatRef(ResourceReference? r)
        => r == null ? "(none)" : $"{r.Reference}{(string.IsNullOrWhiteSpace(r.Display) ? "" : $" ({r.Display})")}";

    static string FormatRefs(IEnumerable<ResourceReference>? rs)
        => rs == null ? "(none)" : string.Join(", ", rs.Select(FormatRef));

    static string ListCodingSystems(CodeableConcept? cc)
    {
        if (cc?.Coding == null || cc.Coding.Count == 0) return "(none)";
        return string.Join(", ", cc.Coding.Where(c => !string.IsNullOrWhiteSpace(c.System)).Select(c => c.System).Distinct());
    }

    // 依 category / performer / specimen 等做「實務上常用」的領域推論（不是標準規則，但很常用）
    static string InferDomainHint(Observation obs)
    {
        // 1) category 常用：vital-signs/laboratory/imaging/social-history
        var categorySystems = obs.Category?
            .SelectMany(c => c.Coding ?? new List<Coding>())
            .Where(c => c.System == "http://terminology.hl7.org/CodeSystem/observation-category")
            .Select(c => c.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList() ?? new List<string>();

        if (categorySystems.Contains("vital-signs"))
            return "Likely Vital Signs (often recorded by nursing devices/staff); units usually UCUM.";
        if (categorySystems.Contains("laboratory"))
            return "Likely Laboratory result (often from LIS); specimen commonly present; code usually LOINC; units often UCUM.";
        if (categorySystems.Contains("imaging"))
            return "Likely Imaging-related observation (may be derivedFrom ImagingStudy/DiagnosticReport).";
        if (categorySystems.Contains("social-history"))
            return "Likely Social History (e.g., smoking, alcohol).";

        // 2) specimen 有時可視為檢驗/病理類訊號
        if (obs.Specimen != null)
            return "Specimen present -> often lab/pathology workflow.";

        // 3) performer 是 Organization / PractitionerRole 時，常用來推測科室/檢驗單位
        if (obs.Performer?.Any(p => p.Reference != null && p.Reference.StartsWith("Organization/")) == true)
            return "Performer includes Organization -> may indicate department/lab organization.";

        return "No strong hint; use category + code system + performer/specimen to classify.";
    }
}
