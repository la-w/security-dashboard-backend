using Microsoft.CodeAnalysis.Sarif;
using SecurityDashboard_Backend.DomainModel;

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
}
