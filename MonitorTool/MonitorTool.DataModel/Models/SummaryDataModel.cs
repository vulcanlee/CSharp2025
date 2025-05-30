using MonitorTool.Share.Helpers;

namespace MonitorTool.DataModel.Models;

public class SummaryDataModel
{
    public string Title { get; set; }
    public string HostsCount { get; set; }
    public string TotalHostsColor { get; set; }

    public void BuildSummaryDataModel(int hostsCount, 
        SummaryDataModelEnum summaryDataModelEnumType)
    {
        HostsCount = hostsCount.ToString();
        if(summaryDataModelEnumType == SummaryDataModelEnum.正常主機)
        {
            Title = "監控機台總數";
            TotalHostsColor = MagicObjectHelper.TotalNormalHostColor;
        }
        else if (summaryDataModelEnumType == SummaryDataModelEnum.異常主機)
        {
            Title = "異常機台總數";
            TotalHostsColor = MagicObjectHelper.TotalAbormalHostColor;
        }
    }
}

public enum SummaryDataModelEnum
{
    正常主機,
    異常主機
}
