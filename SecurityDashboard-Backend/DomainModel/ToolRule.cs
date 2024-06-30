using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.EntityFrameworkCore;

namespace SecurityDashboard_Backend.DomainModel;

[Index(nameof(RuleId), nameof(Tool), IsUnique = true)]
public class ToolRule
{
    public int Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SarifTool Tool { get; set; }

    public string RuleId { get; set; }
    public string Name { get; set; }
    public FailureLevel? DefaultLevel { get; set; }

    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? HelpText { get; set; }
    public string? HelpUri { get; set; }

    public string? CvssVector { get; set; }

    [JsonIgnore]
    public List<ToolFinding> Findings { get; set; } = new();
}
