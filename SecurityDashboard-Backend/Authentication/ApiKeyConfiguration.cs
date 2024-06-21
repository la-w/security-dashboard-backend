using System.Diagnostics.CodeAnalysis;

namespace SecurityDashboard_Backend.Authentication;

[ExcludeFromCodeCoverage(Justification = "Just a config class")]
public class ApiKeyConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
}
