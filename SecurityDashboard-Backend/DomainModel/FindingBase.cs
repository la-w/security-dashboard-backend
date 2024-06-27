using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Sarif;

namespace SecurityDashboard_Backend.DomainModel;

public abstract class FindingBase
{
    public long Id { get; set; }

    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FailureLevel Level { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DetectionType DetectionType { get; set; }


    public List<string> CweIds { get; set; } = new();
    public List<string> SecurityAdvisories { get; set; } = new();
}

public enum DetectionType
{
    SAST,
    SECRET,
    SCA,
    DAST
}
