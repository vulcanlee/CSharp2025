using csListEncounter.Models;
using Fhir.Metrics;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Utility;
using System.Diagnostics.Metrics;
using Task = System.Threading.Tasks.Task;

namespace csListEncounter;

internal class Program
{
    static PatientConditions patientConditions = new PatientConditions();
    static PatientEncounters patientEncounters = new PatientEncounters();
    static ConditionSample conditionSample = new ConditionSample();

    private const string FhirBaseUrl = "https://hapi.fhir.org/baseR4";
    //private const string FhirBaseUrl = "https://server.fire.ly";
    const int maxPatients = 1000;
    static bool showProcessing = false;

    static async Task Main(string[] args)
    {
        // GET https://hapi.fhir.org/baseR4/Condition?patient=Patient/623673&encounter=Encounter/623679
        // GET https://hapi.fhir.org/baseR4/Condition?subject:missing=false&encounter:missing=false
        // GET https://hapi.fhir.org/baseR4/Encounter?patient=Patient/{patientId}&_include=Encounter:patient&_revinclude=Condition:encounter

        //var conditionPatients = await CollectPatientWithCondition();
        //Console.WriteLine($"-----------------------------------------");
        //var encounterPatients = await CollectPatientWithEncounter();

        // 建立帶有 logging 功能的 FhirClient
        var loggingHandler = new LoggingHandler(new HttpClientHandler());
        
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };
        
        var client = new FhirClient(new Uri(FhirBaseUrl), settings, loggingHandler);

        var patientId = "";
        var encounterId = "";
        patientId = "623673";
        patientId = "622898";
        patientId = "623673";
        patientId = "47969400";
        patientId = "1398961";

        encounterId = "1398964";
        await ListEncounter(client, patientId);
        //await ListCondition(client, patientId);
        //await ListEncounterAndConditionByPatientId(client, patientId);
        //await ListConditionByPatientAndEncounter(client, patientId, encounterId);

        //encounterId = "47969439";
        //Console.WriteLine($"-----------------------------------------");

        //await ListCondition(client, patientId);
        //await GetConditionByEncounterId("623673");

        //await ListConditionHasPatientAndEncounterReference(client);
    }

    static async Task ListEncounterAndConditionByPatientId(FhirClient client, string patientId)
    {
        // 第一步：查詢此病人的所有 Encounter
        var encounterSearch = new SearchParams()
            .Where($"patient=Patient/{patientId}")
            .LimitTo(200);

        var encounterBundle = await client.SearchAsync<Encounter>(encounterSearch);

        conditionSample.ConditionNodes.Clear();

        // 暫存 Encounter 資訊：Code / Text / Start
        var encounterMap = new Dictionary<string, (string? Code, string? CodeText, string? Start)>();

        while (encounterBundle != null)
        {
            foreach (var entry in encounterBundle.Entry)
            {
                Console.Write("*");

                if (entry.Resource is not Encounter encounter)
                {
                    continue;
                }

                var encounterId = encounter.Id;
                if (string.IsNullOrWhiteSpace(encounterId))
                {
                    continue;
                }

                var classCode = encounter.Class?.Code;
                var classText = encounter.Class?.Display;
                var start = encounter.Period?.Start;

                encounterMap[encounterId] = (classCode, classText, start);
            }

            if (encounterBundle.NextLink != null)
            {
                encounterBundle = await client.ContinueAsync(encounterBundle);
            }
            else
            {
                encounterBundle = null;
            }
        }

        // 第二步：對每個 Encounter 查 Condition?encounter=Encounter/{id}
        foreach (var kvp in encounterMap)
        {
                    Console.Write("%");
            var encounterId = kvp.Key;
            var (encCode, encCodeText, encStart) = kvp.Value;

            var conditionSearch = new SearchParams()
                .Where($"encounter=Encounter/{encounterId}")
                .LimitTo(200);

            var conditionBundle = await client.SearchAsync<Condition>(conditionSearch);

            while (conditionBundle != null)
            {
                foreach (var entry in conditionBundle.Entry)
                {
                    Console.Write("=");
                    if (entry.Resource is not Condition condition)
                    {
                        continue;
                    }

                    var conditionId = condition.Id ?? "(no id)";

                    // 解析 PatientId（Condition.subject）
                    var patientRef = condition.Subject?.Reference;
                    string? parsedPatientId = null;
                    const string patientPrefix = "Patient/";
                    if (!string.IsNullOrWhiteSpace(patientRef) &&
                        patientRef.StartsWith(patientPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        parsedPatientId = patientRef.Substring(patientPrefix.Length);
                    }

                    // 保險起見，只收這個病人的資料
                    if (parsedPatientId != patientId)
                    {
                        continue;
                    }

                    var codeFirstCoding = condition.Code?.Coding?.FirstOrDefault();
                    var code = codeFirstCoding?.Code;
                    var display = codeFirstCoding?.Display;
                    var recordedDate = condition.RecordedDate;

                    var item = new ConditionNodeSample
                    {
                        PatientId = parsedPatientId,
                        EncounterId = encounterId,
                        EncounterCode = encCode,
                        EncounterCodeText = encCodeText,
                        EncounterStart = encStart,
                        ConditionId = conditionId,
                        ConditionCode = code,
                        Display = display,
                        RecordedDate = recordedDate?.ToString()
                    };

                    conditionSample.ConditionNodes.Add(item);
                }

                if (conditionBundle.NextLink != null)
                {
                    conditionBundle = await client.ContinueAsync(conditionBundle);
                }
                else
                {
                    conditionBundle = null;
                }
            }
        }

        // 若需要輸出檢查
        foreach (var item in conditionSample.ConditionNodes
                     .OrderBy(x => x.EncounterId)
                     .ThenBy(x => x.ConditionId))
        {
            Console.WriteLine(
                $"PatientId={item.PatientId}, " +
                $"EncounterId={item.EncounterId}, " +
                $"EncounterCode={item.EncounterCode}, EncounterCodeText={item.EncounterCodeText}, EncounterStart={item.EncounterStart}, " +
                $"ConditionId={item.ConditionId}, " +
                $"ConditionCode={item.ConditionCode}, Display={item.Display}, RecordedDate={item.RecordedDate}");
        }
    }
    static async Task ListConditionHasPatientAndEncounterReference(FhirClient client)
    {
        // 對應：
        // GET https://hapi.fhir.org/baseR4/Condition?subject:missing=false&encounter:missing=false
        var searchParams = new SearchParams()
            .Where("subject:missing=false")
            .Where("encounter:missing=false")
            .LimitTo(200);

        var bundle = await client.SearchAsync<Condition>(searchParams);

        int cc = 0;
        while (bundle != null)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is not Condition condition)
                {
                    continue;
                }

                var conditionId = condition.Id ?? "(no id)";

                // Patient Reference: Condition.subject (e.g. "Patient/623673")
                var patientRef = condition.Subject?.Reference ?? string.Empty;
                // Encounter Reference: Condition.encounter (e.g. "Encounter/623679")
                var encounterRef = condition.Encounter?.Reference ?? string.Empty;

                // 解析出純 Id
                string? patientId = null;
                string? encounterId = null;

                const string patientPrefix = "Patient/";
                const string encounterPrefix = "Encounter/";

                if (!string.IsNullOrWhiteSpace(patientRef) &&
                    patientRef.StartsWith(patientPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    patientId = patientRef.Substring(patientPrefix.Length);
                }

                if (!string.IsNullOrWhiteSpace(encounterRef) &&
                    encounterRef.StartsWith(encounterPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    encounterId = encounterRef.Substring(encounterPrefix.Length);
                }

                var codeFirstCoding = condition.Code?.Coding?.FirstOrDefault();
                var code = codeFirstCoding?.Code;
                var display = codeFirstCoding?.Display;
                var recordedDate = condition.RecordedDate;

                if(string.IsNullOrWhiteSpace(patientId) || string.IsNullOrWhiteSpace(encounterId))
                {
                    continue; // 無法解析出 PatientId 或 EncounterId 就跳過
                }
                // 存到你的 sample model
                var item = new ConditionNodeSample
                {
                    ConditionId = conditionId,
                    PatientId = patientId,
                    EncounterId = encounterId,
                    ConditionCode = code,
                    Display = display,
                    RecordedDate = recordedDate?.ToString()
                };
                conditionSample.ConditionNodes.Add(item);

                //Console.WriteLine(
                //    $"PatientId={patientId}, ConditionId={conditionId}, EncounterId={encounterId}, " +
                //    $"Code={code}, Display={display}, RecordedDate={recordedDate}");

                cc++;
                if (cc % 100 == 0)
                {
                    Console.WriteLine($"Processed {cc} Condition resources...");
                }
                if (cc > 3000)
                {
                    break;
                }
            }

            if (cc > 3000)
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

        var all = conditionSample.ConditionNodes
            .GroupBy(x => x.PatientId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .SelectMany(g => g
                .OrderBy(x => x.EncounterId)
                .ThenBy(x => x.ConditionId));
        // 做出排序，最多的 PatientId 在前面，接著按照 EncounterId 與 ConditionId 排序

        foreach (var item in all)
        {
            //Console.WriteLine(
            //    $"PatientId={patientId}, ConditionId={conditionId}, EncounterId={encounterId}, " +
            //    $"Code={code}, Display={display}, RecordedDate={recordedDate}");
            Console.WriteLine(
                $"PatientId={item.PatientId}, ConditionId={item.ConditionId}, EncounterId={item.EncounterId}, " +
                $"Code={item.ConditionCode}, Display={item.Display}, RecordedDate={item.RecordedDate}");
        }
    }
    // 依 patientId + encounterId 取得相關 Condition
    static async Task ListConditionByPatientAndEncounter(FhirClient client, string patientId, string encounterId)
    {
        // 等價於：
        // GET https://hapi.fhir.org/baseR4/Condition?patient=Patient/{patientId}&encounter=Encounter/{encounterId}
        var searchParams = new SearchParams()
            .Where($"patient=Patient/{patientId}")
            .Where($"encounter=Encounter/{encounterId}")
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
                var codeFirstCoding = condition.Code?.Coding?.FirstOrDefault();
                var code = codeFirstCoding?.Code;
                var display = codeFirstCoding?.Display;
                var recordedDate = condition.RecordedDate;

                Console.WriteLine(
                    $"   ConditionId={conditionId}, Code={code}, Display={display}, RecordedDate={recordedDate}");
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
        PatientConditionNode item = new PatientConditionNode()
        {
            PatientId = patientId,
            Items = new List<ConditionNode>()
        };

        patientConditions.Items.Add(item);

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
                var recordedDate = condition.RecordedDate; // 例如 "2020-01-01"item

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
        PatientEncounterNode item = new()
        {
            PatientId = patientId,
            Items = new List<EncounterNode>()
        };
        patientEncounters.Items.Add(item);

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

                item.Items.Add(new EncounterNode()
                {
                    Id = encounterId,
                    Code = classCode,
                    CodeText = typeText,
                    Start = start,
                    End = end
                });

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

    static async Task GetConditionByEncounterId(FhirClient client, string encounterId)
    {
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
