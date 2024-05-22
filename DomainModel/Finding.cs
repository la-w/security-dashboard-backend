using Microsoft.EntityFrameworkCore;

namespace SecurityDashboard_Backend.DomainModel;

[Index(nameof(Cvss31Score))]
[Index(nameof(Cvss40Score))]
public abstract class Finding
{
    public int Id { get; set; }

    public string Location { get; set; }
    public SarifTool Tool { get; set; }
    public string RuleId { get; set; }
    public string Description { get; set; }
    public string Snippet { get; set; }
    public int SnippetStartLine { get; set; }
    public int? SnippetEndLine { get; set; }

    public decimal? Cvss31Score { get; set; }
    public string? Cvss31Vector { get; set; }
    public decimal? Cvss40Score { get; set; }
    public string? Cvss40Vector { get; set; }
    public List<string> CweIds { get; set; } = new();
    public List<string> SecurityAdvisories { get; set; } = new();

    public int PipelineRunId { get; set; }
    public PipelineRun PipelineRun { get; set; }
}

public enum SarifTool
{
    FluidAttacks,
    GitLeaks,
    OwaspDependencyCheck
}
