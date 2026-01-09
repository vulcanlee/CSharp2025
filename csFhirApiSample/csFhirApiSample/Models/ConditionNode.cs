namespace csFhirApiSample.Models;

public class ConditionNode
{
    public string Id { get; set; }
    // Classification of patient encounter (Symbol in syntax defined by the system)
    public string ClassCode { get; set; }
    // 
    public string ClassCodeDesc { get; set; }
    public string RecordedDate { get; set; }
}
