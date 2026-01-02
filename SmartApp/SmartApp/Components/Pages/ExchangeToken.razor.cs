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
    [Inject]
    public SmartAppSettingService SmartAppSettingService { get; init; }

    public async System.Threading.Tasks.Task SetAuthCodeAsync()
    {
        await System.Threading.Tasks.Task.Yield();
        SmartAppSettingService.Data.AuthCode = Code;
    }

    public async System.Threading.Tasks.Task<SmartResponse> GetAccessTokenAsync()
    {
        Dictionary<string, string> requestValues = new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "code", SmartAppSettingService.Data.AuthCode },
                { "redirect_uri", SmartAppSettingService.Data.RedirectUrl },
                { "scope", "openid fhirUser patient/*.read launch" },  // EHR launch 用 "launch"
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

        // 2. 用這個 HttpClient 建立 FhirClient
        FhirClientSettings settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };

        FhirClient fhirClient = new FhirClient(SmartAppSettingService.Data.FhirServerUrl, httpClient, settings);

        // 3. 讀取 Patient
        patient = await fhirClient.ReadAsync<Patient>($"Patient/{smartResponse.PatientId}");

        System.Console.WriteLine($"Read back patient: {patient.Name[0].ToString()}");

        isReadPatient = true;
    }
}
