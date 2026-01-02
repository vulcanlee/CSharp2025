using Microsoft.Extensions.Options;
using SmartApp.Models;

namespace SmartApp.Servicers;

public class SmartAppSettingService
{
    private readonly SettingService settingService;
    public SmartAppSettingModel Data = new SmartAppSettingModel();

    public SmartAppSettingService(SettingService settingService)
    {
        this.settingService = settingService;

        var data = settingService.GetValue();
        Data.FhirServerUrl = data.FhirServerUrl;
        Data.RedirectUrl = data.RedirectUrl;
        Data.ClientId = data.ClientId;
    }
}
