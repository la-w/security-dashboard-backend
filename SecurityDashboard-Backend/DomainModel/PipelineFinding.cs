namespace SecurityDashboard_Backend.DomainModel;

public class PipelineFinding : FindingBase
{
    public List<ToolFinding> Findings { get; set; } = new();

    public bool IsFalsePositive { get; set; }

    public long PipelineRunId { get; set; }
    public PipelineRun PipelineRun { get; set; }

    public int ToolRuleId { get; set; }
    public ToolRule ToolRule { get; set; }
}
