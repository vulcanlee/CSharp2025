using MonitorTool.Share.Helpers;

namespace MonitorTool.DataModel.Models;

public class MonitorTypeModel
{
    public string Name { get; set; }
    public string Title { get; set; }
    public MonitorTypeNodeCategoryEnum MonitorTypeNodeCategory { get; set; }
    public int NormalNodes { get; set; }
    public int AbnormalNodes { get; set; }
    public string NormalPercentTitle { get; set; }
    public string NormalPercent { get; set; }
    public string AbnormalPercent { get; set; }
    public double AbnormalPercentValue { get; set; }
    public string NormalColor { get; set; }
    public string AbnormalColor { get; set; }

    public int MaxShowNode { get; set; } = 12;

    public List<MonitorTypeNodeModel> Nodes { get; set; } = new();

    public void Build(MonitorTypeNodeCategoryEnum monitorTypeNodeCategoryEnum)
    {
        Nodes.Clear();
        #region 建立名稱
        if (monitorTypeNodeCategoryEnum == MonitorTypeNodeCategoryEnum.Service)
        {
            Name = "服務";
            NormalColor = MagicObjectHelper.DonutColor服務Normal;
            AbnormalColor = MagicObjectHelper.DonutColor服務Abnormal;
        }
        else if (monitorTypeNodeCategoryEnum == MonitorTypeNodeCategoryEnum.Memory)
        {
            Name = "記憶體";
            NormalColor = MagicObjectHelper.DonutColorMemoryNormal;
            AbnormalColor = MagicObjectHelper.DonutColorMemoryAbnormal;
        }
        else if (monitorTypeNodeCategoryEnum == MonitorTypeNodeCategoryEnum.Disk)
        {
            Name = "磁碟空間";
            NormalColor = MagicObjectHelper.DonutColorDiskNormal;
            AbnormalColor = MagicObjectHelper.DonutColorDiskAbnormal;
        }
        else if (monitorTypeNodeCategoryEnum == MonitorTypeNodeCategoryEnum.Cpu)
        {
            Name = "處理器";
            NormalColor = MagicObjectHelper.DonutColorCpuNormal;
            AbnormalColor = MagicObjectHelper.DonutColorCpuAbnormal;
        }
        else if (monitorTypeNodeCategoryEnum == MonitorTypeNodeCategoryEnum.Database)
        {
            Name = "資料庫";
            NormalColor = MagicObjectHelper.DonutColor資料庫Normal;
            AbnormalColor = MagicObjectHelper.DonutColor資料庫Abnormal;
        }
        else if (monitorTypeNodeCategoryEnum == MonitorTypeNodeCategoryEnum.DatabaseSpace)
        {
            Name = "資料庫空間";
            NormalColor = MagicObjectHelper.DonutColor資料庫空間Normal;
            AbnormalColor = MagicObjectHelper.DonutColor資料庫空間Abnormal;
        }
        #endregion
        MonitorTypeNodeCategory = monitorTypeNodeCategoryEnum;
        int hostsCount = new Random().Next(8, 20);
        int maxAbnormalNodeCount = new Random().Next(1, 5);
        for (int i = 0; i < hostsCount; i++)
        {
            Nodes.Add(new MonitorTypeNodeModel
            {
                Title = $"{Name}{i + 1}",
                Status = i < maxAbnormalNodeCount ? MonitorTypeNodeStatusEnum.異常主機 : MonitorTypeNodeStatusEnum.正常主機,
            });
        }

        Refresh();
    }

    void Refresh()
    {
        Title = $"{Name}({Nodes.Count})";
        NormalNodes = Nodes.Count(n => n.Status == MonitorTypeNodeStatusEnum.正常主機);
        AbnormalNodes = Nodes.Count(n => n.Status == MonitorTypeNodeStatusEnum.異常主機);
        int hostsCount = NormalNodes + AbnormalNodes;
        NormalPercent = ((int)((double)(NormalNodes) / hostsCount * 100.0)).ToString();
        AbnormalPercent = ((int)((double)(AbnormalNodes) / hostsCount * 100.0)).ToString();
        AbnormalPercentValue = (double)(AbnormalNodes) / hostsCount;
        NormalPercentTitle = $"{NormalPercent}%";
        foreach (var node in Nodes)
        {
            if (node.Status == MonitorTypeNodeStatusEnum.正常主機)
            {
                node.StatusColor = MagicObjectHelper.TotalNormalHostColor;
            }
            else
            {
                node.StatusColor = MagicObjectHelper.TotalAbormalHostColor;
            }
        }
    }

    public List<MonitorTypeNodeModel> GetDisplayList()
    {
        List<MonitorTypeNodeModel> result = new();
        result = Nodes.Take(MaxShowNode).ToList();
        if(Nodes.Count < MaxShowNode)
        {
            int count = MaxShowNode - Nodes.Count;
            for (int i = 0; i < count; i++)
            {
                result.Add(new MonitorTypeNodeModel
                {
                    Title = $"_",
                    Status = MonitorTypeNodeStatusEnum.正常主機,
                    StatusColor = MagicObjectHelper.TotalNormalHostColor
                });
            }
        }
        return result;
    }

    public class MonitorTypeNodeModel
    {
        public string Title { get; set; }
        public MonitorTypeNodeStatusEnum Status { get; set; }
        public string StatusColor { get; set; }
    }
}

public enum MonitorTypeNodeCategoryEnum
{
    Service,
    Memory,
    Disk,
    Cpu,
    Database,
    DatabaseSpace,
}

public enum MonitorTypeNodeStatusEnum
{
    正常主機,
    異常主機
}
