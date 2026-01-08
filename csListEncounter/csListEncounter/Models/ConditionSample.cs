namespace csListEncounter.Models;

public class ConditionSample
{
    public List<ConditionNodeSample> ConditionNodes { get; set; } = new();
}
public class ConditionNodeSample
{
    public string PatientId { get; set; }
    public string EncounterId { get; set; }
    public string ConditionId { get; set; }
    public string Code { get; set; }
    public string Display { get; set; }
    public string RecordedDate { get; set; }
}

