namespace csListEncounter.Models;

public class PatientHasEncountes
{
    public List<PatientConditions> Items { get; set; } = new();
}
public class PatientHasEncounterNode
{
    public string PatientId { get; set; }
    public int Count { get; set; } = 0;
}
