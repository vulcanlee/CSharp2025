namespace csListEncounter.Models;

public class PatientEncounters
{
    public List<PatientEncounterNode> Items { get; set; } = new();
}

public class PatientEncounterNode
{
    public string PatientId { get; set; }
    public List<EncounterNode> Items { get; set; }
}

public class EncounterNode
{
    public string Id { get; set; }
    // Symbol in syntax defined by the system
    public string Code { get; set; }
    // 
    public string CodeText { get; set; }
    // Starting time with inclusive boundary
    public string Start { get; set; }
    // End time with inclusive boundary, if not ongoing
    public string End { get; set; }
}
