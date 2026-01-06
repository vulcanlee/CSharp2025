using Microsoft.Extensions.Options;
using SmartEHRLaunch1.Models;

namespace SmartEHRLaunch1.Servicers;

public class SettingService
{
    private readonly SettingModel settingModel;

    public SettingService(IOptions<SettingModel> options)
    {
        settingModel = options.Value;
    }

    public SettingModel GetValue()
    {
        return settingModel;
    }
}
