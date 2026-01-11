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

    /// <summary>
    /// 元件第一次渲染後執行的生命週期方法
    /// 負責處理 SMART on FHIR 啟動流程：保存啟動參數、取得 FHIR 伺服器元資料、產生授權 URL 並導向授權伺服器
    /// </summary>
    /// <param name="firstRender">是否為第一次渲染</param>
    /// <returns>非同步任務</returns>
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

    /// <summary>
    /// 保存 SMART on FHIR 啟動參數 (iss 和 launch)
    /// 從查詢字串中取得 iss (FHIR 伺服器 URL) 和 launch (啟動代碼) 參數並儲存到應用程式設定中
    /// </summary>
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

    /// <summary>
    /// 從 FHIR 伺服器取得元資料 (CapabilityStatement)
    /// 解析 SMART on FHIR 的 OAuth 端點 (authorize 和 token URL) 並儲存到應用程式設定中
    /// </summary>
    /// <returns>若成功取得授權和令牌端點則回傳 true，否則回傳 false</returns>
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

    /// <summary>
    /// 產生 OAuth 授權 URL
    /// 建立 state 參數以防止 CSRF 攻擊，將應用程式設定儲存到狀態存儲中，並組合完整的授權 URL
    /// 包含必要的 OAuth 參數：response_type、client_id、redirect_uri、scope、state、launch 和 aud
    /// </summary>
    /// <returns>完整的授權 URL 字串</returns>
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
