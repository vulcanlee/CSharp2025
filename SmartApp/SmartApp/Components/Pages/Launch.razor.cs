using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Components;
using SmartApp.Servicers;

namespace SmartApp.Components.Pages;

public partial class Launch
{
    // https://localhost:7191/launch

    // https://localhost:7191/launch?iss=https://thas.mohw.gov.tw/v/r4/fhir&launch=WzAsIiIsIiIsIkFVVE8iLDAsMCwwLCIiLCIiLCIiLCIiLCIiLCIiLCIiLDAsMSwiIl0
    [Inject]
    public SmartAppSettingService SmartAppSettingService { get; init; }

    public void KeepLaunchIss()
    {
        SmartAppSettingService.Data.Iss = Iss;
        SmartAppSettingService.Data.Launch = LaunchCode;
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

    public string GetAuthorizeUrl()
    {
        string launchUrl = $"{SmartAppSettingService.Data.AuthorizeUrl}?response_type=code" +
            $"&client_id={SmartAppSettingService.Data.ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(SmartAppSettingService.Data.RedirectUrl)}" +
            $"&scope={Uri.EscapeDataString("openid fhirUser patient/*.read launch")}" +
            $"&state={SmartAppSettingService.Data.ClientState}" +
            $"&launch={SmartAppSettingService.Data.Launch}" +
            $"&aud={Uri.EscapeDataString(SmartAppSettingService.Data.FhirServerUrl)}" ;
        return launchUrl;
    }
}
