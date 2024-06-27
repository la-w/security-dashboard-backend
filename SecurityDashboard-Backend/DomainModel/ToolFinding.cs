using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SecurityDashboard_Backend.DomainModel;

[JsonDerivedType(typeof(FluidAttacksFinding))]
[JsonDerivedType(typeof(GitLeaksFinding))]
[JsonDerivedType(typeof(OwaspDependencyCheckFinding))]
[JsonDerivedType(typeof(SemgrepFinding))]
[JsonDerivedType(typeof(TrivyFinding))]
[JsonDerivedType(typeof(OwaspZapFinding))]
public abstract class ToolFinding : FindingBase
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SarifTool Tool { get; set; }

    public string Snippet { get; set; } = string.Empty;
    public int SnippetStartLine { get; set; }
    public int? SnippetStartColumn { get; set; }
    public int? SnippetEndLine { get; set; }
    public int? SnippetEndColumn { get; set; }

    public long PipelineRunId { get; set; }
    public PipelineRun PipelineRun { get; set; }

    public int ToolRuleId { get; set; }
    public ToolRule ToolRule { get; set; }

    public long? PipelineFindingId { get; set; }
    public PipelineFinding? PipelineFinding { get; set; }
}

public enum SarifTool
{
    [Display(Name = "FluidAttacks Scanner")]
    FluidAttacks,

    [Display(Name = "OWASP ZAP")]
    OwaspZap,

    [Display(Name = "GitLeaks")]
    GitLeaks,

    [Display(Name = "OWASP Dependency-Check")]
    OwaspDependencyCheck,

    [Display(Name = "Aqua Security Trivy")]
    Trivy,

    [Display(Name = "Semgrep OSS")]
    Semgrep,
}
