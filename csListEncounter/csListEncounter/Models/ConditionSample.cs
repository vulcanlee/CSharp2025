namespace csListEncounter.Models;

public class ConditionSample
{
    public List<ConditionNodeSample> ConditionNodes { get; set; } = new();
}
public class ConditionNodeSample
{
    public string PatientId { get; set; }
    public string EncounterId { get; set; }
    // Symbol in syntax defined by the system
    public string EncounterCode { get; set; }
    // 
    public string EncounterCodeText { get; set; }
    public string EncounterStart { get; set; }

    public string ConditionId { get; set; }
    public string ConditionCode { get; set; }
    public string Display { get; set; }
    public string RecordedDate { get; set; }
}

