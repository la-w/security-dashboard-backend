using SecurityDashboard_Backend.DomainModel;
using System.ComponentModel.DataAnnotations;

namespace SecurityDashboard_Backend.Models;

public class PipelineRunDto
{
    public int PipelineId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(300)]
    public string Branch { get; set; }

    [MinLength(40), MaxLength(40)]
    public string CommitHash { get; set; }

    public RepositoryDto Repository { get; set; }
}
