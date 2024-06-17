using Microsoft.CodeAnalysis.Sarif;

namespace SecurityDashboard_Backend.Services;

public interface ISarifImportService
{
    Task ImportAsync(SarifLog sarifLog, int pipelineId);
}
