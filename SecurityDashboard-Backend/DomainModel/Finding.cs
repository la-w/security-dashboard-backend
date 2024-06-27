using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis.Sarif;

namespace SecurityDashboard_Backend.DomainModel;

public abstract class Finding
{
    public int Id { get; set; }

    public string Location { get; set; } = string.Empty;
    public SarifTool Tool { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public int SnippetStartLine { get; set; }
    public int? SnippetStartColumn { get; set; }
    public int? SnippetEndLine { get; set; }
    public int? SnippetEndColumn { get; set; }
    public FailureLevel Level { get; set; }

    public List<string> CweIds { get; set; } = new();
    public List<string> SecurityAdvisories { get; set; } = new();

    public int PipelineRunId { get; set; }
    public PipelineRun PipelineRun { get; set; }

    public int ToolRuleId { get; set; }
    public ToolRule ToolRule { get; set; }
}

public enum SarifTool
{
    [Display(Name = "FluidAttacks Scanner")]
    FluidAttacks,

    [Display(Name = "GitLeaks")]
    GitLeaks,

    [Display(Name = "OWASP Dependency-Check")]
    OwaspDependencyCheck,

    [Display(Name = "Semgrep OSS")]
    Semgrep,

    [Display(Name = "Aqua Security Trivy")]
    Trivy,

    [Display(Name = "OWASP ZAP")]
    OwaspZap
}
