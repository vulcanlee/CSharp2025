using csFhirApiSample.Helpers;
using csFhirApiSample.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System.Net.Http.Headers;
using System.Text;

namespace csFhirApiSample.Services;

public class ConditionService
{
    public async System.Threading.Tasks.Task<List<ConditionNode>> GetByEncounterAsync(string encounterId)
    {
        List<ConditionNode> result = new List<ConditionNode>();

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

        var searchParams = new SearchParams()
            .Where($"encounter=Encounter/{encounterId}")
            .LimitTo(200);

        var conditionBundle = await fhirClient.SearchAsync<Condition>(searchParams);

        while (conditionBundle != null)
        {
            foreach (var entry in conditionBundle.Entry)
            {
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

                var codeFirstCoding = condition.Code?.Coding?.FirstOrDefault();
                var code = codeFirstCoding?.Code;
                var display = codeFirstCoding?.Display;
                var recordedDate = condition.RecordedDate;

                var item = new ConditionNode
                {
                    Id = conditionId,
                    ClassCode = code ?? "(no code)",
                    ClassCodeDesc = display ?? "(no display)",
                    RecordedDate = recordedDate,
                };

                result.Add(item);
            }

            if (conditionBundle.NextLink != null)
            {
                conditionBundle = await fhirClient.ContinueAsync(conditionBundle);
            }
            else
            {
                conditionBundle = null;
            }
        }
        return result;

    }

    public async System.Threading.Tasks.Task<List<ConditionNode>> GetByPatientAsync(string patientId)
    {
        List<ConditionNode> result = new List<ConditionNode>();

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

        var searchParams = new SearchParams()
            .Where($"patient=Patient/{patientId}")
            .LimitTo(200);

        var conditionBundle = await fhirClient.SearchAsync<Condition>(searchParams);

        while (conditionBundle != null)
        {
            foreach (var entry in conditionBundle.Entry)
            {
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

                var codeFirstCoding = condition.Code?.Coding?.FirstOrDefault();
                var code = codeFirstCoding?.Code;
                var display = codeFirstCoding?.Display;
                var recordedDate = condition.RecordedDate;

                var item = new ConditionNode
                {
                    Id = conditionId,
                    ClassCode = code ?? "(no code)",
                    ClassCodeDesc = display ?? "(no display)",
                    RecordedDate = recordedDate,
                };

                result.Add(item);
            }

            if (conditionBundle.NextLink != null)
            {
                conditionBundle = await fhirClient.ContinueAsync(conditionBundle);
            }
            else
            {
                conditionBundle = null;
            }
        }
        return result;
    }
}
