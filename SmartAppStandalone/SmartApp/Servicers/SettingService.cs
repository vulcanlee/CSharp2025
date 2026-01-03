using Microsoft.Extensions.Options;
using SmartApp.Models;

namespace SmartApp.Servicers;

public class SettingService
{
    private readonly SettingModel  settingModel;

    public SettingService(IOptions<SettingModel> options)
    {
        settingModel = options.Value;
    }

    public SettingModel GetValue()
    {
        return settingModel;
    }
}
