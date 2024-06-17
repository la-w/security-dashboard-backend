using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Sarif;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.Utilities;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Models;
using SecurityDashboard_Backend.Services;

namespace SecurityDashboard_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SarifController : ControllerBase
{
    private readonly ILogger<SarifController> _logger;
    private readonly SecDashboardContext _context;

    private readonly ISarifImportService _importService;

    public SarifController(ILogger<SarifController> logger, SecDashboardContext context, ISarifImportService importService)
    {
        _logger = logger;
        _context = context;
        _importService = importService;
    }

    [HttpPost("RegisterPipelineRun")]
    [ServiceFilter(typeof(RemoteIpCheckActionFilter))]
    [Authorize(Policy = Policies.UsedApiKey)]
    public async Task<IActionResult> PostRegisterPipelineRunAsync([FromBody] PipelineRunDto pipelineRunDto)
    {
        if (await _context.PipelineRuns.AnyAsync(p => p.PipelineId == pipelineRunDto.PipelineId))
            return Conflict("Pipeline run already exists!");

        var repository = await _context.Repositories.FirstOrDefaultAsync(r => r.Url == pipelineRunDto.Repository.Url);

        if (repository == null)
        {
            repository = new Repository()
            {
                Name = pipelineRunDto.Repository.Name,
                Url = pipelineRunDto.Repository.Url
            };
        }
        else if (repository.Name != pipelineRunDto.Repository.Name)
        {
            repository.Name = pipelineRunDto.Repository.Name;
        }

        var pipelineRun = new PipelineRun()
        {
            PipelineId = pipelineRunDto.PipelineId,
            CreatedAt = pipelineRunDto.CreatedAt,
            Branch = pipelineRunDto.Branch,
            CommitHash = pipelineRunDto.CommitHash,
            Repository = repository
        };

        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("RegisterReport")]
    [ServiceFilter(typeof(RemoteIpCheckActionFilter))]
    [Authorize(Policy = Policies.UsedApiKey)]
    public async Task<IActionResult> PostRegisterReportAsync([FromBody] SarifLog log, [FromQuery, Required] int pipelineId)
    {
        if (log.Runs == null || log.Runs.Count == 0)
            return BadRequest("The log does not contain any runs.");

        if (log.Runs.Count > 1)
            return BadRequest("Import currently supports only Single-Run-files.");

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

        try
        {
            await _importService.ImportAsync(log, pipelineId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }

        return Ok();
    }
}
