using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Components;
using SmartApp.Models;
using SmartApp.Servicers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SmartApp.Components.Pages;

public partial class ExchangeToken
{
    VitalSignsResult heightAndWeight = new VitalSignsResult();
    List<OrderItemResult> OrderItemResult = new ();
    [Inject]    
    public SmartAppSettingService SmartAppSettingService { get; init; }
    [Inject]
    public OAuthStateStoreService OAuthStateStoreService { get; init; }

    protected override async System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetAuthCodeAsync();
            SmartResponse smartResponse = await GetAccessTokenAsync();
            await GetPatientAsync(smartResponse);
            heightAndWeight = await GetHeightAndWeightAsync(smartResponse); 
            await GetLastYearOrdersAsync(smartResponse);
            StateHasChanged();
        }
    }

    public async System.Threading.Tasks.Task SetAuthCodeAsync()
    {
        await System.Threading.Tasks.Task.Yield();
       var SmartAppSettingModelItem =  await OAuthStateStoreService.LoadAsync<SmartAppSettingModel>(State);

        SmartAppSettingModelItem.AuthCode = Code;
        SmartAppSettingModelItem.State = State;

        SmartAppSettingService.UpdateSetting(SmartAppSettingModelItem);
        Console.WriteLine($"Retrive state: {SmartAppSettingService.Data.State}");
    }

    public async System.Threading.Tasks.Task<SmartResponse> GetAccessTokenAsync()
    {
        Dictionary<string, string> requestValues = new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "code", SmartAppSettingService.Data.AuthCode },
                { "redirect_uri", SmartAppSettingService.Data.RedirectUrl },
                { "launch", SmartAppSettingService.Data.Launch }
            };

        HttpRequestMessage request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(SmartAppSettingService.Data.TokenUrl),
            Content = new FormUrlEncodedContent(requestValues),
        };

        HttpClient client = new HttpClient();

        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            System.Console.WriteLine($"Failed to exchange code for token!");
            throw new Exception($"Unauthorized: {response.StatusCode}");
        }

        string json = await response.Content.ReadAsStringAsync();

        System.Console.WriteLine($"----- Authorization Response -----");
        System.Console.WriteLine(json);
        System.Console.WriteLine($"----- Authorization Response -----");

        SmartResponse smartResponse = JsonSerializer.Deserialize<SmartResponse>(json);
        return smartResponse;
    }

    public async System.Threading.Tasks.Task GetPatientAsync(SmartResponse smartResponse)
    {
        // 1. 先建立 HttpClient，預設好 Authorization header
        HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(SmartAppSettingService.Data.FhirServerUrl)
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", smartResponse.AccessToken);

        FhirClientSettings settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };

        FhirClient fhirClient = new FhirClient(SmartAppSettingService.Data.FhirServerUrl, httpClient, settings);

        patient = await fhirClient.ReadAsync<Patient>($"Patient/{smartResponse.PatientId}");

        System.Console.WriteLine($"Read back patient: {patient.Name[0].ToString()}");

        isReadPatient = true;
    }

    private async System.Threading.Tasks.Task<VitalSignsResult> GetHeightAndWeightAsync(SmartResponse smartResponse)
    {
        HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(SmartAppSettingService.Data.FhirServerUrl)
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", smartResponse.AccessToken);

        FhirClientSettings settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };

        FhirClient fhirClient = new FhirClient(SmartAppSettingService.Data.FhirServerUrl, httpClient, settings);

        // 查詢該病人的 Observation（限制常見 vital-sign codes）
        SearchParams searchParams = new SearchParams()
            .Where($"patient={smartResponse.PatientId}")
            .Where("category=vital-signs")
            .Include("Observation:patient")
            .LimitTo(50);

        Bundle bundle = await fhirClient.SearchAsync<Observation>(searchParams);

        decimal? heightValue = null;
        string? heightUnit = null;
        decimal? weightValue = null;
        string? weightUnit = null;

        foreach (Bundle.EntryComponent entry in bundle.Entry)
        {
            if (entry.Resource is not Observation obs)
            {
                continue;
            }

            // Observation.code.coding[].code 比對 LOINC
            string? loincCode = obs.Code?.Coding?.FirstOrDefault()?.Code;

            if (loincCode is null)
            {
                continue;
            }

            Quantity? quantity = obs.Value as Quantity;
            if (quantity is null)
            {
                continue;
            }

            if (loincCode == "8302-2")
            {
                // 身高
                if (quantity.Value.HasValue)
                {
                    heightValue = (decimal)quantity.Value.Value;
                    heightUnit = quantity.Unit ?? quantity.Code;
                }
            }
            else if (loincCode == "29463-7")
            {
                // 體重
                if (quantity.Value.HasValue)
                {
                    weightValue = (decimal)quantity.Value.Value;
                    weightUnit = quantity.Unit ?? quantity.Code;
                }
            }
        }

        System.Console.WriteLine($"Height: {heightValue} {heightUnit}");
        System.Console.WriteLine($"Weight: {weightValue} {weightUnit}");

        return new VitalSignsResult
        {
            HeightValue = heightValue?.ToString(),
            HeightUnit = heightUnit?.ToString(),
            WeightValue = weightValue?.ToString(),
            WeightUnit = weightUnit?.ToString()
        };
    }

    /// <summary>
    /// 取得特定病患最近一年內，門診 / 急診 / 住院相關 Encounter 的醫令清單（含醫令代碼與意義）
    /// 這裡示範抓 MedicationRequest + ServiceRequest，實際可再依需求加入 Procedure / DiagnosticReport 等。
    /// </summary>
    private async System.Threading.Tasks.Task<List<OrderItemResult>> GetLastYearOrdersAsync(SmartResponse smartResponse)
    {
        DateTimeOffset today = DateTimeOffset.UtcNow;
        DateTimeOffset oneYearAgo = today.AddYears(-30);

        HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(SmartAppSettingService.Data.FhirServerUrl)
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", smartResponse.AccessToken);

        FhirClientSettings settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json,
        };

        FhirClient fhirClient = new FhirClient(
            SmartAppSettingService.Data.FhirServerUrl,
            httpClient,
            settings);


        // 1. 先找出該病患最近一年內的 Encounter
        // date=ge{一年前}&date=le{今天}，再加 patient=xxx
        SearchParams encounterSearch = new SearchParams()
            .Where($"patient={smartResponse.PatientId}")
            //.Where($"date=ge{oneYearAgo:O}")
            //.Where($"date=le{today:O}")
            .LimitTo(100);

        var list = encounterSearch.ToUriParamList();
        var query = string.Join("&", list.Select(p => $"{p.Item1}={p.Item2}"));
        string fullUrl = $"{SmartAppSettingService.Data.FhirServerUrl}/Encounter?{query}";
        System.Console.WriteLine($"FHIR Search URL: {fullUrl}");

        Bundle encounterBundle = await fhirClient.SearchAsync<Encounter>(encounterSearch);

        List<Encounter> encounters = new List<Encounter>();

        foreach (Bundle.EntryComponent entry in encounterBundle.Entry)
        {
            if (entry.Resource is Encounter enc)
            {
                encounters.Add(enc);
            }
        }

        // 如果有分頁，可視需要把下一頁也抓回來 (這裡簡化未處理)

        // 2. 過濾門診 / 急診 / 住院
        //    Encounter.class.code / type.coding[].code 的 mapping 會依各家實作不同，你要依實際情況調整。
        List<Encounter> filteredEncounters = encounters
            //.Where(e => IsOpdErIpdEncounter(e))
            .ToList();

        List<OrderItemResult> results = new List<OrderItemResult>();

        foreach (Encounter encounter in filteredEncounters)
        {
            string encounterId = encounter.Id;

            string encounterType = GetEncounterType(encounter);

            DateTimeOffset? start = encounter.Period?.StartElement?.ToDateTimeOffset(TimeSpan.FromHours(8));

            // 3a. 找這個 Encounter 相關的 MedicationRequest（用 encounter=EncounterId）
            SearchParams mrSearch = new SearchParams()
                .Where($"encounter={encounterId}")
                .LimitTo(100);

            Bundle mrBundle = await fhirClient.SearchAsync<MedicationRequest>(mrSearch);
            foreach (Bundle.EntryComponent entry in mrBundle.Entry)
            {
                if (entry.Resource is not MedicationRequest mr)
                {
                    continue;
                }

                // medication 是 choice type：可能是 CodeableConcept 或 ResourceReference
                // 這裡先處理成 CodeableConcept 的情境
                CodeableConcept? medConcept = mr.Medication as CodeableConcept;

                if (medConcept == null)
                {
                    // 如果是 Reference (例如 Reference reference = mr.Medication as ResourceReference)
                    // 可以在這裡再決定要不要另外 Read<Medication> 把 code 抓回來
                    continue;
                }

                Coding? coding = medConcept.Coding?.FirstOrDefault();
                if (coding is null)
                {
                    continue;
                }

                results.Add(new OrderItemResult
                {
                    EncounterId = encounterId,
                    EncounterType = encounterType,
                    EncounterStart = start,
                    OrderResourceType = "MedicationRequest",
                    OrderId = mr.Id,
                    OrderCode = coding.Code,
                    OrderDisplay = coding.Display
                });
            }

            // 3b. 找這個 Encounter 相關的 ServiceRequest
            SearchParams srSearch = new SearchParams()
                .Where($"encounter={encounterId}")
                .LimitTo(100);

            Bundle srBundle = await fhirClient.SearchAsync<ServiceRequest>(srSearch);
            foreach (Bundle.EntryComponent entry in srBundle.Entry)
            {
                if (entry.Resource is not ServiceRequest sr)
                {
                    continue;
                }

                Coding? coding = sr.Code?.Coding?.FirstOrDefault();
                if (coding is null)
                {
                    continue;
                }

                results.Add(new OrderItemResult
                {
                    EncounterId = encounterId,
                    EncounterType = encounterType,
                    EncounterStart = start,
                    OrderResourceType = "ServiceRequest",
                    OrderId = sr.Id,
                    OrderCode = coding.Code,
                    OrderDisplay = coding.Display
                });
            }
        }

        // 依就醫日期排序，越新的在前
        List<OrderItemResult> orderedResults = results
            .OrderByDescending(r => r.EncounterStart)
            .ToList();

        foreach (OrderItemResult item in orderedResults)
        {
            System.Console.WriteLine(
                $"Encounter={item.EncounterId}({item.EncounterType}) " +
                $"OrderType={item.OrderResourceType} Code={item.OrderCode} Display={item.OrderDisplay}");
        }

        return orderedResults;
    }

    /// <summary>
    /// 判斷 Encounter 是否為門診 / 急診 / 住院。
    /// 真正的判斷規則要依實際 FHIR 伺服器的 coding 慣例調整。
    /// 這裡只示意用 Encounter.Class.Code 或 Type.Coding.Code 來區分。
    /// </summary>
    public bool IsOpdErIpdEncounter(Encounter encounter)
    {
        string? cls = encounter.Class?.Code;

        // 以下的 code 只是舉例，你需要依照實際 coding 規格（如 HL7 v2, local code set）調整
        if (string.Equals(cls, "AMB", StringComparison.OrdinalIgnoreCase))
        {
            // Ambulatory / 門診
            return true;
        }

        if (string.Equals(cls, "EMER", StringComparison.OrdinalIgnoreCase))
        {
            // Emergency / 急診
            return true;
        }

        if (string.Equals(cls, "IMP", StringComparison.OrdinalIgnoreCase))
        {
            // Inpatient / 住院
            return true;
        }

        // 也可以再看 Type.Coding 裡的 code 來判斷
        if (encounter.Type != null)
        {
            foreach (CodeableConcept type in encounter.Type)
            {
                Coding? coding = type.Coding?.FirstOrDefault();
                if (coding == null)
                {
                    continue;
                }

                string? code = coding.Code;

                // 依實際系統定義調整
                if (string.Equals(code, "OPD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(code, "ER", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(code, "IPD", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 回傳簡化後的 Encounter 類型字串（OPD/ER/IPD/...）
    /// </summary>
    public string GetEncounterType(Encounter encounter)
    {
        string? cls = encounter.Class?.Code;

        if (string.Equals(cls, "AMB", StringComparison.OrdinalIgnoreCase))
        {
            return "OPD";
        }

        if (string.Equals(cls, "EMER", StringComparison.OrdinalIgnoreCase))
        {
            return "ER";
        }

        if (string.Equals(cls, "IMP", StringComparison.OrdinalIgnoreCase))
        {
            return "IPD";
        }

        // 也可以從 Type.Coding 推斷
        if (encounter.Type != null)
        {
            foreach (CodeableConcept type in encounter.Type)
            {
                Coding? coding = type.Coding?.FirstOrDefault();
                if (coding?.Code != null)
                {
                    return coding.Code;
                }
            }
        }

        return string.Empty;
    }
}
