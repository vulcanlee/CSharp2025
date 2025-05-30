using ShareLibrary;

namespace DataModel;

public class PercentDataModel
{
    public string Title { get; set; }
    public int NormalHostsCount { get; set; }
    public int AbormalHostsCount { get; set; }
    public int NormalHostsPercent { get; set; }
    public int AbormalHostsPercent { get; set; }
    public string NormalHostsColor { get; set; }
    public string AbormalHostColor { get; set; }
    public string NormalHostsSummary { get; set; }
    public string AbormalHostSummary { get; set; }

    public void Build(int normalHostsCount, int abnormalHostsCount)
    {
        NormalHostsColor = MagicObjectHelper.TotalNormalHostColor;
        AbormalHostColor = MagicObjectHelper.TotalAbormalHostColor;
        NormalHostsCount = normalHostsCount;
        AbormalHostsCount = abnormalHostsCount;
        int totalHostsCount = NormalHostsCount + AbormalHostsCount;
        if (totalHostsCount == 0)
        {
            NormalHostsPercent = 0;
            AbormalHostsPercent = 0;
        }
        else
        {
            NormalHostsPercent = (int)((double)NormalHostsCount / totalHostsCount * 100);
            AbormalHostsPercent = (int)((double)AbormalHostsCount / totalHostsCount * 100);
        }

        NormalHostsSummary = $"正常：{NormalHostsCount}台 ({NormalHostsPercent}%)";
        AbormalHostSummary = $"異常：{AbormalHostsCount}台 ({AbormalHostsPercent}%)";
    }
}

