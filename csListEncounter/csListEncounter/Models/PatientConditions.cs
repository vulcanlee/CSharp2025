namespace csListEncounter.Models;

public class PatientConditions
{
    public List<PatientConditionNode> Items { get; set; } = new();
}

public class PatientConditionNode
{
    public string PatientId { get; set; }
    public List<ConditionNode> Items { get; set; }
}

public class ConditionNode
{
    public string Id { get; set; }
    // Classification of patient encounter (Symbol in syntax defined by the system)
    public string ClassCode { get; set; }
    // 
    public string ClassCodeDesc { get; set; }
    // Starting time with inclusive boundary
    public string Start { get; set; }
    // End time with inclusive boundary, if not ongoing
    public string End { get; set; }
}
