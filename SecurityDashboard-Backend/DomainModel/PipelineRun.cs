using System.ComponentModel.DataAnnotations;

namespace SecurityDashboard_Backend.DomainModel;

public class PipelineRun
{
    public long Id { get; set; }

    public long PipelineId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(300)]
    public string Branch { get; set; }

    [MinLength(40), MaxLength(40)]
    public string CommitHash { get; set; }

    public int RepositoryId { get; set; }
    public Repository Repository { get; set; }

    public List<ToolFinding> ToolFindings { get; set; } = new();
    public List<PipelineFinding> PipelineFindings { get; set; } = new();
}
