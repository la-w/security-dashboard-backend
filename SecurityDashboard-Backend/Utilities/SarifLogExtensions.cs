using System.Net;
using System.Text;
using Microsoft.CodeAnalysis.Sarif;
using SecurityDashboard_Backend.DomainModel;
using WebRequest = Microsoft.CodeAnalysis.Sarif.WebRequest;
using WebResponse = Microsoft.CodeAnalysis.Sarif.WebResponse;

namespace SecurityDashboard_Backend.Utilities;

public static class SarifLogExtensions
{
    public static SarifTool? DetermineTool(this SarifLog log)
    {
        if (log.Runs.Count == 0)
            throw new ArgumentException("The log does not contain any runs.", nameof(log));

        return log.Runs[0].Tool.Driver.Name switch
        {
            "skims" => SarifTool.FluidAttacks,
            "Semgrep OSS" => SarifTool.Semgrep,
            "gitleaks" => SarifTool.GitLeaks,
            "dependency-check" => SarifTool.OwaspDependencyCheck,
            "Trivy" => SarifTool.Trivy,
            "ZAP" => SarifTool.OwaspZap,
            _ => null
        };
    }


    public static string GetStringRepresentation(this WebRequest webRequest)
    {
        if (webRequest == null)
            throw new ArgumentNullException(nameof(webRequest));

        var sb = new StringBuilder();
        sb.AppendLine("=== GENERAL ===");
        sb.AppendLine($"Protocol: {webRequest.Protocol}; Version: {webRequest.Version}; Method: {webRequest.Method}");
        sb.AppendLine($"Target: {webRequest.Target}");
        sb.AppendLine();
        sb.AppendLine("=== HEADERS ===");
        foreach (var header in webRequest.Headers)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("=== BODY ===");
        var body = webRequest.Body?.Text ?? webRequest.Body?.Rendered?.Text;
        sb.AppendLine(!string.IsNullOrEmpty(body) ? body : ":: No body ::");

        return sb.ToString();
    }

    public static string GetStringRepresentation(this WebResponse webResponse)
    {
        if (webResponse == null)
            throw new ArgumentNullException(nameof(webResponse));

        if (webResponse.NoResponseReceived)
            return ":: No Response Received ::";

        var sb = new StringBuilder();
        sb.AppendLine("=== GENERAL ===");
        sb.AppendLine($"Protocol: {webResponse.Protocol}; Version: {webResponse.Version}");
        sb.AppendLine($"Status: [{webResponse.StatusCode}] {webResponse.ReasonPhrase}");
        sb.AppendLine();
        sb.AppendLine("=== HEADERS ===");
        foreach (var header in webResponse.Headers)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }

        sb.AppendLine();
        sb.AppendLine("=== BODY ===");
        var body = webResponse.Body?.Text ?? webResponse.Body?.Rendered?.Text;
        sb.AppendLine(!string.IsNullOrEmpty(body) ? body : ":: No body ::");

        return sb.ToString();
    }
}
