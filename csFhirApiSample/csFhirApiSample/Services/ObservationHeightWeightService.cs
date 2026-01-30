using csFhirApiSample.Helpers;
using csFhirApiSample.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Model.CdsHooks;
using Hl7.Fhir.Rest;
using System.Net.Http.Headers;

namespace csFhirApiSample.Services;

public class ObservationHeightWeightService
{
    public async System.Threading.Tasks.Task<PatientModel> GetAsync(PatientModel patientModel)
    {
        PatientModel result = patientModel;
        // 1. 先建立 HttpClient，預設好 Authorization header
        HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri(MagicObjectHelper.FhirBaseUrl)
        };

        FhirClientSettings settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };

        FhirClient fhirClient = new FhirClient(MagicObjectHelper.FhirBaseUrl, httpClient, settings);

        // 查詢該病人的 Observation（限制常見 vital-sign codes）
        SearchParams searchParams = new SearchParams()
            .Where($"patient={result.Id}")
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

            if (loincCode == "8302-2")
            {
                Quantity? quantity = obs.Value as Quantity;
                if (quantity is null)
                {
                    continue;
                }

                // 身高
                if (quantity.Value.HasValue)
                {
                    result.HeightValue = quantity.Value?.ToString();
                    result.HeightUnit = quantity.Unit ?? quantity.Code;
                }
            }
            else if (loincCode == "29463-7")
            {
                Quantity? quantity = obs.Value as Quantity;
                if (quantity is null)
                {
                    continue;
                }

                // 體重
                if (quantity.Value.HasValue)
                {
                    result.WeightValue = quantity.Value.ToString();
                    result.WeightUnit = quantity.Unit ?? quantity.Code;
                }
            }// 3. 血型：假設血型 Observation 的 code 是 882-1，且 value 是 CodeableConcept
            else if (loincCode == "882-1") // TODO: 這個 code 要換成你們實際使用的 LOINC / Code
            {
                // valueCodeableConcept
                CodeableConcept? cc = obs.Value as CodeableConcept;
                if (cc is not null)
                {
                    // 例如 Coding[0].Code 可能是 "A", "B", "AB", "O"
                    string? bloodTypeCode = cc.Coding?.FirstOrDefault()?.Code;
                    if (!string.IsNullOrEmpty(bloodTypeCode))
                    {
                        result.BloodType = bloodTypeCode;
                    }
                }

                // 如果伺服器用 valueString，則改用：
                // FhirString? bloodTypeString = obs.Value as FhirString;
                // if (bloodTypeString is not null)
                // {
                //     result.BloodType = bloodTypeString.Value;
                // }
            }
        }

        return patientModel;
    }
}
