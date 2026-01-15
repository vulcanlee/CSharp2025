using Microsoft.Extensions.Options;
using SmartStandalone1.Models;

namespace SmartStandalone1.Servicers;

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
