using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public List<ToolFinding> ToolFindings { get; set; } = new();

    [JsonIgnore]
    public List<PipelineFinding> PipelineFindings { get; set; } = new();
}
