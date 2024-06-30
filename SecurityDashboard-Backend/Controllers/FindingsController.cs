using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.Authentication;

namespace SecurityDashboard_Backend.Controllers;

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

        var findingsQuery = _context.ToolFindings.AsNoTracking()
            .Where(f => f.PipelineRunId == pipelineRun.Id)
            .AsAsyncEnumerable();

        return Ok(findingsQuery);
    }

    [HttpPost]
    [Route("SetFalsePositive")]
    [Authorize(Policies.OAuthUser)]
    public async Task<IActionResult> PostSetFalsePositiveAsync(long findingId, bool isFalsePositive)
    {
        if (findingId <= 0)
            return BadRequest("Invalid findingId.");

        var pipelineFinding = await _context.PipelineFindings.FindAsync(findingId);

        if (pipelineFinding == null)
            return BadRequest("PipelineFinding does not exist.");

        pipelineFinding.IsFalsePositive = isFalsePositive;

        await _context.SaveChangesAsync();

        return Ok();
    }
}
