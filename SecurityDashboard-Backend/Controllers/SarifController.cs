using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Sarif;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.Utilities;
using System.ComponentModel.DataAnnotations;

namespace SecurityDashboard_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SarifController : ControllerBase
{
    private readonly ILogger<SarifController> _logger;

    public SarifController(ILogger<SarifController> logger)
    {
        _logger = logger;
    }

    [HttpPost("RegisterReport")]
    [ServiceFilter(typeof(RemoteIpCheckActionFilter))]
    [Authorize(Policy = Policies.UsedApiKey)]
    public async Task<IActionResult> PostRegisterReportAsync([FromBody] SarifLog log, [FromQuery, Required] int pipelineId)
    {
        if (log.Runs == null || log.Runs.Count == 0)
            return BadRequest("The log does not contain any runs.");

        // "skims" -> FluidAttacks (SAST, SCA, DAST)
        // "Semgrep OSS" -> Semgrep (SAST)
        // "gitleaks" -> GitLeaks (SAST / Secret Detection)
        // "dependency-check" -> OWASP Dependency Check (SCA)
        // "Trivy" -> Trivy (Container Scanning)
        // "ZAP" -> OWASP ZAP (DAST)
        var sarifToolName = log.Runs[0].Tool.Driver.Name;
        var sarifTool = log.DetermineTool();

        if (sarifTool == null)
            return BadRequest("Unknown SARIF tool.");

        _logger.LogInformation("Received SARIF from Tool {ShortName} (-> {DetectedTool}) in Pipeline #{PipelineId}", sarifToolName, sarifTool,
            pipelineId);

        return Ok(log);
    }
}
