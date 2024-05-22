using System.ComponentModel.DataAnnotations;

namespace SecurityDashboard_Backend.DomainModel;

public class PipelineRun
{
    public int Id { get; set; }

    public int PipelineId { get; set; }

    [MaxLength(300)]
    public string Branch { get; set; }

    [MinLength(40), MaxLength(40)]
    public string CommitHash { get; set; }

    public int RepositoryId { get; set; }
    public Repository Repository { get; set; }

    public List<Finding> Findings { get; set; }
}
