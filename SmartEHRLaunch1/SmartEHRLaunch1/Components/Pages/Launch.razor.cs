using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Components;
using SmartEHRLaunch1.Models;
using SmartEHRLaunch1.Servicers;

namespace SmartEHRLaunch1.Components.Pages;

public partial class Launch
{
    [Inject]
    public NavigationManager NavigationManager { get; init; }
    [Inject]
    public SmartAppSettingService SmartAppSettingService { get; init; }
    [Inject]
    public OAuthStateStoreService OAuthStateStoreService { get; init; }

    string IssMessage = string.Empty;
    string LaunchMessage = string.Empty;
    string authUrlMessage = string.Empty;

    protected override async System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            KeepLaunchIss();
            var bar = await GetMetadataAsync();
            var authUrl = await GetAuthorizeUrlAsync();
            authUrlMessage = $"重新導向到授權伺服器:{authUrl}";

            StateHasChanged();

            await System.Threading.Tasks.Task.Delay(5000);

            NavigationManager.NavigateTo(authUrl);
        }
    }

    public void KeepLaunchIss()
    {
        if (string.IsNullOrEmpty(Iss) || string.IsNullOrEmpty(LaunchCode))
        {
            SmartAppSettingService.Data.Iss = null;
            SmartAppSettingService.Data.Launch = null;
            return;
        }
        SmartAppSettingService.Data.Iss = Iss;
        SmartAppSettingService.Data.Launch = LaunchCode;
        SmartAppSettingService.Data.FhirServerUrl = Iss;
    }

    public async Task<bool> GetMetadataAsync()
    {
        Hl7.Fhir.Rest.FhirClient fhirClient = new Hl7.Fhir.Rest.FhirClient(SmartAppSettingService.Data.FhirServerUrl);

        CapabilityStatement capabilities = (CapabilityStatement)(await fhirClient.GetAsync("metadata"));

        foreach (CapabilityStatement.RestComponent restComponent in capabilities.Rest)
        {
            if (restComponent.Security == null)
            {
                continue;
            }

            foreach (Extension securityExt in restComponent.Security.Extension)
            {
                if (securityExt.Url != "http://fhir-registry.smarthealthit.org/StructureDefinition/oauth-uris")
                {
                    continue;
                }

                if ((securityExt.Extension == null) || (securityExt.Extension.Count == 0))
                {
                    continue;
                }

                foreach (Extension smartExt in securityExt.Extension)
                {
                    switch (smartExt.Url)
                    {
                        case "authorize":
                            SmartAppSettingService.Data.AuthorizeUrl = ((FhirUri)smartExt.Value).Value.ToString();
                            break;

                        case "token":
                            SmartAppSettingService.Data.TokenUrl = ((FhirUri)smartExt.Value).Value.ToString();
                            break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(SmartAppSettingService.Data.AuthorizeUrl) || string.IsNullOrEmpty(SmartAppSettingService.Data.TokenUrl))
        {
            return false;
        }

        return true;
    }

    public async System.Threading.Tasks.Task<string> GetAuthorizeUrlAsync()
    {
        var state = Guid.NewGuid().ToString("N");
        SmartAppSettingService.Data.State = state;

        await OAuthStateStoreService.SaveAsync<SmartAppSettingModel>(state, SmartAppSettingService.Data, TimeSpan.FromMinutes(10));

        Console.WriteLine($"Generated state: {SmartAppSettingService.Data.State}");
        string launchUrl = $"{SmartAppSettingService.Data.AuthorizeUrl}?response_type=code" +
            $"&client_id={SmartAppSettingService.Data.ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(SmartAppSettingService.Data.RedirectUrl)}" +
            $"&scope={Uri.EscapeDataString("openid fhirUser profile launch/patient patient/*.read patient/Encounter.read patient/MedicationRequest.read patient/ServiceRequest.read")}" +
            $"&state={SmartAppSettingService.Data.State}" +
            $"&launch={SmartAppSettingService.Data.Launch}" +
            $"&aud={Uri.EscapeDataString(SmartAppSettingService.Data.FhirServerUrl)}";
        authUrlMessage = $"重新導向到授權伺服器:{launchUrl}";
        return launchUrl;
    }
}
