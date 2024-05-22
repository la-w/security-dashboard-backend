namespace SecurityDashboard_Backend.DomainModel;

public class FluidAttacksFinding : Finding
{
    public string Kind { get; set; }
    public string Technique { get; set; }
    public string SourceMethod { get; set; }
    public string DocumentationLink { get; set; }
}
