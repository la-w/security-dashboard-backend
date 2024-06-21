using SecurityDashboard_Backend.DomainModel;
using System.ComponentModel.DataAnnotations;

namespace SecurityDashboard_Backend.Models;

/// <summary>
/// Pipeline Run Data
/// </summary>
public class PipelineRunDto
{
    /// <summary>
    /// (GitLab Global) Pipeline-ID
    /// </summary>
    /// <example>1234</example>
    public int PipelineId { get; set; }

    /// <summary>
    /// Date and Time when the Pipeline was created
    /// </summary>
    /// <example>2024-12-31T23:59:59Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Branch of the Repository
    /// </summary>
    /// <example>main</example>
    [MaxLength(300)]
    public string Branch { get; set; }

    /// <summary>
    /// Hash of the Commit that triggered the Pipeline
    /// </summary>
    /// <example>1234567890abcdef1234567890abcdef12345678</example>
    [MinLength(40), MaxLength(40)]
    public string CommitHash { get; set; }

    /// <summary>
    /// Repository Data
    /// </summary>
    public RepositoryDto Repository { get; set; }
}
