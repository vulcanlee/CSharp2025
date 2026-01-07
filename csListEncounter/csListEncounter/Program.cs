using Fhir.Metrics;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using System.Diagnostics.Metrics;
using Task = System.Threading.Tasks.Task;

namespace csListEncounter;

public class PatientHasEncounterNode
{
    public string PatientId { get; set; }
    public int Count { get; set; } = 0;
}

public class EncounterHasCoditionNode
{
    public string PatientId { get; set; }
    public int Count { get; set; } = 0;
}

internal class Program
{
    private const string FhirBaseUrl = "https://hapi.fhir.org/baseR4";
    //private const string FhirBaseUrl = "https://server.fire.ly";
    const int maxPatients = 1000;
    static bool showProcessing = false;

    static async Task Main(string[] args)
    {
        // GET https://hapi.fhir.org/baseR4/Condition?patient=Patient/623673&encounter=Encounter/623679

        //var conditionPatients = await CollectPatientWithCondition();
        //Console.WriteLine($"-----------------------------------------");
        //var encounterPatients = await CollectPatientWithEncounter();

        var client = new FhirClient(FhirBaseUrl);
        var patientId = "";
        patientId = "623673";
        patientId = "622898";
        patientId = "623673";
        //patientId = "ae61f37c-14ed-47dd-ad89-efb57c106227";
        await ListEncounter(client, patientId);
        Console.WriteLine($"-----------------------------------------");
        //await ListCondition(client, patientId);
        //await GetConditionByEncounterId("623673");
    }

    // 參考 CollectPatientWithEncounter：
    // 來源改成 Condition，從 Condition.subject 反推 Patient，
    // 統計每個 Patient 擁有幾筆 Condition，最後印出前 10 名。
    static async Task<List<PatientHasEncounterNode>> CollectPatientWithCondition()
    {
        var client = new FhirClient(FhirBaseUrl);

        var patientIds = new List<PatientHasEncounterNode>();

        var searchParams = new SearchParams()
            .LimitTo(100); // 每頁最多 100 筆 Condition（實務上 100 已不少）

        var bundle = await client.SearchAsync<Condition>(searchParams);

        while (bundle != null && patientIds.Count < maxPatients)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Condition condition)
                {
                    continue;
                }

                // Condition.subject 指向 Patient 的 Reference，例如 "Patient/123"
                var subjectRef = condition.Subject?.Reference;
                if (string.IsNullOrWhiteSpace(subjectRef))
                {
                    continue;
                }

                const string patientPrefix = "Patient/";
                if (!subjectRef.StartsWith(patientPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var patientId = subjectRef.Substring(patientPrefix.Length);
                if (string.IsNullOrWhiteSpace(patientId))
                {
                    continue;
                }

                var existingNode = patientIds.FirstOrDefault(x => x.PatientId == patientId);
                if (existingNode != null)
                {
                    existingNode.Count++;
                    if (showProcessing)
                        Console.Write("@"); // 已經收集過這個病人，累加 Condition 次數
                    continue;
                }

                patientIds.Add(new PatientHasEncounterNode
                {
                    PatientId = patientId,
                    Count = 1
                });

                if (showProcessing)
                    Console.Write("."); // 新病人

                if (patientIds.Count >= maxPatients)
                {
                    break;
                }
            }

            if (patientIds.Count >= maxPatients)
            {
                break;
            }

            if (bundle.NextLink != null)
            {
                bundle = await client.ContinueAsync(bundle);
            }
            else
            {
                bundle = null;
            }
        }

        Console.WriteLine($"{Environment.NewLine}Collected Patient IDs by Condition (top 10):");
        var sortedPatientIds = patientIds
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        foreach (var patientNode in sortedPatientIds)
        {
            Console.WriteLine($"{patientNode.PatientId} / ConditionCount={patientNode.Count}");
            await ListCondition(client, patientNode.PatientId);
        }

        return patientIds;
    }

    static async Task<List<PatientHasEncounterNode>> CollectPatientWithEncounter()
    {
        var client = new FhirClient(FhirBaseUrl);

        List<PatientHasEncounterNode> patientIds = new List<PatientHasEncounterNode>();
        var searchParams = new SearchParams()
            .LimitTo(10000); // 每頁最多 100 筆 Encounter

        var bundle = await client.SearchAsync<Encounter>(searchParams);

        while (bundle != null && patientIds.Count < maxPatients)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Encounter encounter)
                {
                    continue;
                }

                // Encounter.subject 應該是指向 Patient 的 Reference，例如 "Patient/123"
                var subjectRef = encounter.Subject?.Reference;
                if (string.IsNullOrWhiteSpace(subjectRef))
                {
                    continue;
                }

                // 只處理以 "Patient/" 開頭的 reference
                const string patientPrefix = "Patient/";
                if (!subjectRef.StartsWith(patientPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var patientId = subjectRef.Substring(patientPrefix.Length);
                if (string.IsNullOrWhiteSpace(patientId))
                {
                    continue;
                }

                var existingNode = patientIds.FirstOrDefault(x => x.PatientId == patientId);
                if (existingNode != null)
                {
                    existingNode.Count++;
                    if (showProcessing)
                        Console.Write("@"); // 已經收集過了
                    continue; // 已經收集過了
                }
                patientIds.Add(new PatientHasEncounterNode() { PatientId = patientId, Count = 1 });

                if (showProcessing)
                    Console.Write(".");

                if (patientIds.Count >= maxPatients)
                {
                    break;
                }
            }

            if (patientIds.Count >= maxPatients)
            {
                break;
            }

            // 有下一頁就繼續
            if (bundle.NextLink != null)
            {
                bundle = await client.ContinueAsync(bundle);
            }
            else
            {
                bundle = null;
            }
        }

        Console.WriteLine($"{Environment.NewLine}Collected Patient IDs (up to 10):");
        var sortedPatientIds = patientIds.OrderByDescending(x => x.Count).Take(10).ToList();
        foreach (var partientNode in sortedPatientIds)
        {
            Console.WriteLine($"{partientNode.PatientId} / {partientNode.Count}");

            await ListEncounter(client, partientNode.PatientId);
        }

        return patientIds;
    }

    // 檢查指定 patient 是否至少有一筆 Encounter
    static async Task<bool> HasEncounter(FhirClient client, string patientId)
    {
        var searchParams = new SearchParams()
            .Where($"patient={patientId}")
            .LimitTo(1); // 只要知道有沒有，不用全抓

        var bundle = await client.SearchAsync<Encounter>(searchParams);
        return bundle.Entry.Any();
    }

    static async Task ListCondition(FhirClient client, string patientId)
    {
        // Search: Condition?subject=Patient/{id}&_count=200
        var searchParams = new SearchParams()
            .Where($"subject=Patient/{patientId}")
            .LimitTo(200);

        var bundle = await client.SearchAsync<Condition>(searchParams);

        while (bundle != null)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Condition condition)
                {
                    continue;
                }

                var conditionId = condition.Id ?? "(no id)";

                // 主要診斷代碼與顯示文字
                var codeText = condition.Code?.Text;
                var codeFirstCoding = condition.Code?.Coding?.FirstOrDefault();
                var system = codeFirstCoding?.System;
                var code = codeFirstCoding?.Code;
                var display = codeFirstCoding?.Display;
                var encounter = condition.Encounter?.Reference;

                // 發病日期/記錄日期
                var onset = condition.Onset switch
                {
                    FhirDateTime fdt => fdt.Value,
                    Period p => $"{p.Start} ~ {p.End}",
                    _ => null
                };

                // Condition.RecordedDate 是 Hl7.Fhir.Model.Date
                var recordedDate = condition.RecordedDate; // 例如 "2020-01-01"

                //Console.WriteLine(
                //    $"   ConditionId={conditionId}, CodeText={codeText}, " +
                //    $"CodingSystem={system}, Code={code}, Display={display}, " +
                //    $"Onset={onset}, RecordedDate={recordedDate}");
                Console.WriteLine(
                    $"   ConditionId={conditionId}, Code={code}, encounter={encounter}, " +
                    $"RecordedDate={recordedDate}");
            }

            if (bundle.NextLink != null)
            {
                bundle = await client.ContinueAsync(bundle);
            }
            else
            {
                bundle = null;
            }
        }
    }

    // 依 patientId 列出所有 Encounter：類別 + EncounterId + 時間
    static async Task ListEncounter(FhirClient client, string patientId)
    {
        // Search: Encounter?patient={id}&_count=200
        var searchParams = new SearchParams()
            .Where($"patient={patientId}")
            .LimitTo(200);

        var bundle = await client.SearchAsync<Encounter>(searchParams);

        while (bundle != null)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Encounter encounter)
                {
                    continue;
                }

                var encounterId = encounter.Id ?? "(no id)";

                var classCode = encounter.Class?.Code;
                var typeText = classCode switch
                {
                    "AMB" => "門診",
                    "EMER" => "急診",
                    "IMP" => "住院",
                    _ => "其他"
                };

                var start = encounter.Period?.Start;
                var end = encounter.Period?.End;

                Console.WriteLine(
                    $"   EncounterId={encounterId}, 類別={typeText}({classCode}), 開始={start}, 結束={end}");
            }

            if (bundle.NextLink != null)
            {
                bundle = await client.ContinueAsync(bundle);
            }
            else
            {
                bundle = null;
            }
        }
    }

    static async Task GetConditionByEncounterId(string encounterId)
    {

        var client = new FhirClient(FhirBaseUrl);
        var searchParams = new SearchParams()
            .Where($"encounter={encounterId}")
            .LimitTo(100); // 每頁最多 100 筆 Condition
        var bundle = await client.SearchAsync<Condition>(searchParams);
        while (bundle != null)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Condition condition)
                {
                    continue;
                }
                Console.WriteLine($"   ConditionId={condition.Id}, Code={condition.Code?.Text}");
            }
            if (bundle.NextLink != null)
            {
                bundle = await client.ContinueAsync(bundle);
            }
            else
            {
                bundle = null;
            }
        }
    }

    static async Task ListPatient()
    {
        var client = new FhirClient(FhirBaseUrl);

        var searchParams = new SearchParams()
            .LimitTo(500); // _count=500

        var searchResult = await client.SearchAsync<Patient>(searchParams);

        foreach (var entry in searchResult.Entry)
        {
            if (entry.Resource is not Patient patient)
            {
                continue;
            }

            // 先查 Encounter，看這個 patient 是否有相關 Encounter
            var hasEncounter = await HasEncounter(client, patient.Id);

            Console.Write(".");
            if (!hasEncounter)
            {
                continue; // 不想顯示沒有 Encounter 的病人就直接略過
            }

            var name = patient.Name?.FirstOrDefault();
            var fullName = name != null
                ? string.Join(" ", name.Given) + " " + name.Family
                : "(no name)";

            Console.WriteLine($"{patient.Id} - {fullName}");
            await ListEncounter(client, patient.Id);
        }
    }
}
