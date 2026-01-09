using csFhirApiSample.Helpers;
using csFhirApiSample.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System.Net.Http.Headers;

namespace csFhirApiSample.Services;

public class EncounterService
{
    public async System.Threading.Tasks.Task<List<EncounterNode>> GetAsync(string patientId)
    {
        List<EncounterNode> result = new List<EncounterNode>();

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

        // Search: Encounter?patient={id}&_count=200
        var searchParams = new SearchParams()
            .Where($"patient={patientId}")
            .LimitTo(200);

        var bundle = await fhirClient.SearchAsync<Encounter>(searchParams);

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

                result.Add(new EncounterNode()
                {
                    Id = encounterId,
                    Code = classCode,
                    CodeText = typeText,
                    Start = start,
                    End = end
                });
            }

            if (bundle.NextLink != null)
            {
                bundle = await fhirClient.ContinueAsync(bundle);
            }
            else
            {
                bundle = null;
            }
        }
        return result;

    }
}
