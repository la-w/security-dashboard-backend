namespace SecurityDashboard_Backend.DomainModel;

public class FluidAttacksFinding : ToolFinding
{
    public string? Cvss31Vector { get; set; }
    public string? Cvss40Vector { get; set; }

    public int? Line { get; set; }
    public string Kind { get; set; }
    public string Technique { get; set; }
    public string SourceMethod { get; set; }
}
