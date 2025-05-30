using MonitorTool.Share.Helpers;

namespace MonitorTool.DataModel.Models;

public class MonitorTarget
{
    public string Title { get; set; }
    public double NormalCount { get; set; }
    public double AbnormalCount { get; set; }
    public double TotalCount => NormalCount + AbnormalCount;
    public double NormalPercent { get; set; }
    public double AbnormalPercent { get; set; }
    public string NormalColor { get; set; }
    public string AbnormalColor { get; set; }

    public void Initialize(MonitorTargetType type)
    {
        Title = type.ToString();
        switch (type)
        {
            case MonitorTargetType.服務:
                NormalColor = MagicObjectHelper.DonutColor服務Normal;
                AbnormalColor = MagicObjectHelper.DonutColor服務Abnormal;
                break;
            case MonitorTargetType.記憶體:
                NormalColor = MagicObjectHelper.DonutColorMemoryNormal;
                AbnormalColor = MagicObjectHelper.DonutColorMemoryAbnormal;
                break;
            case MonitorTargetType.磁碟空間:
                NormalColor = MagicObjectHelper.DonutColorDiskNormal;
                AbnormalColor = MagicObjectHelper.DonutColorDiskAbnormal;
                break;
            case MonitorTargetType.處理器:
                NormalColor = MagicObjectHelper.DonutColorCpuNormal;
                AbnormalColor = MagicObjectHelper.DonutColorCpuAbnormal;
                break;
            case MonitorTargetType.資料庫:
                NormalColor = MagicObjectHelper.DonutColor資料庫Normal;
                AbnormalColor = MagicObjectHelper.DonutColor資料庫Abnormal;
                break;
            case MonitorTargetType.資料庫空間:
                NormalColor = MagicObjectHelper.DonutColor資料庫空間Normal;
                AbnormalColor = MagicObjectHelper.DonutColor資料庫空間Abnormal;
                break;
        }
    }
}

public enum MonitorTargetType
{
    服務,
    記憶體,
    磁碟空間,
    處理器,
    資料庫,
    資料庫空間
}

public class MonitorTargetItem
{
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public double NormalCount { get; set; }
    public double AbnormalCount { get; set; }
    public double TotalCount => NormalCount + AbnormalCount;
    public double NormalPercent { get; set; }
    public double AbnormalPercent { get; set; }
}
