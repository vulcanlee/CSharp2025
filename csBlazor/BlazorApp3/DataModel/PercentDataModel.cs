using ShareLibrary;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public string NormalPathD { get; set; }
    public string AbormalPathD { get; set; }

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

        Refresh();
    }

    public void Refresh()
    {
        // 重新計算路徑
        NormalPathD = GetNormalPathD();
        AbormalPathD = GetAbormalPathD();
    }

    private string GetNormalPathD()
    {
        // 圓心座標
        double cx = 75;
        double cy = 75;
        // 半徑
        double r = 75;

        // 計算對應角度 (轉換百分比為角度)
        double angle = (double)NormalHostsPercent / 100 * 360;

        // 計算終點座標
        double endX = cx + r * Math.Sin(angle * Math.PI / 180);
        double endY = cy - r * Math.Cos(angle * Math.PI / 180);

        // 確定是否為大弧 (角度大於180度)
        int largeArcFlag = angle > 180 ? 1 : 0;

        // 生成路徑描述
        if ((double)NormalHostsPercent >= 100)
        {
            // 如果正常百分比為100%，繪製完整圓
            return $"M {cx} {cy} L {cx} {cy - r} A {r} {r} 0 1 1 {cx - 0.001} {cy - r} Z";
        }
        else if ((double)NormalHostsPercent <= 0)
        {
            // 如果正常百分比為0%，不繪製
            return "";
        }
        else
        {
            // 繪製部分圓弧
            return $"M {cx} {cy} L {cx} {cy - r} A {r} {r} 0 {largeArcFlag} 1 {endX} {endY} L {cx} {cy}";
        }
    }

    private string GetAbormalPathD()
    {
        // 圓心座標
        double cx = 75;
        double cy = 75;
        // 半徑
        double r = 75;

        // 計算正常部分的終止角度
        double normalAngle = (double)NormalHostsPercent / 100 * 360;

        // 計算正常部分的終點座標
        double startX = cx + r * Math.Sin(normalAngle * Math.PI / 180);
        double startY = cy - r * Math.Cos(normalAngle * Math.PI / 180);

        if ((double)AbormalHostsPercent >= 100)
        {
            // 如果異常百分比為100%，繪製完整圓
            return $"M {cx} {cy} L {cx} {cy - r} A {r} {r} 0 1 1 {cx - 0.001} {cy - r} Z";
        }
        else if ((double)AbormalHostsPercent <= 0)
        {
            // 如果異常百分比為0%，不繪製
            return "";
        }
        else
        {
            // 繪製從正常部分結束位置開始的弧
            // 注意：我們必須從正常部分的終點開始繪製
            return $"M {cx} {cy} L {startX} {startY} A {r} {r} 0 {(AbormalHostsPercent > 50 ? 1 : 0)} 1 {cx} {cy - r} L {cx} {cy}";
        }
    }
}

