using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.Authentication;

namespace SecurityDashboard_Backend.Controllers;

[ExcludeFromCodeCoverage(Justification = "Just a Demo-Controller, will be replaced")]
[ApiController]
[Route("api/[controller]")]
public class FindingsController : ControllerBase
{
    private readonly SecDashboardContext _context;

    public FindingsController(SecDashboardContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("Get")]
    [Authorize(Policies.OAuthUser)]
    public async Task<IActionResult> GetFindings(int pipelineId)
    {
        var pipelineRun = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.PipelineId == pipelineId)
            .FirstOrDefaultAsync();

        if (pipelineRun == null)
            return BadRequest("Pipeline not found");

        var findingsQuery = _context.Findings.AsNoTracking()
            .Where(f => f.PipelineRunId == pipelineRun.Id)
            .AsAsyncEnumerable();

        return Ok(findingsQuery);
    }
}
