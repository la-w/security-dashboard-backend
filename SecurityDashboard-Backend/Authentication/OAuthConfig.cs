using System.Diagnostics.CodeAnalysis;

namespace SecurityDashboard_Backend.Authentication;

[ExcludeFromCodeCoverage(Justification = "Just a config class")]
public class OAuthConfig
{
    public string ClientId { get; set; }
    public string Issuer { get; set; }
    public string MetadataAddress { get; set; }
}
