using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Models;

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

    /// <summary>
    /// Returns the findings of the given pipeline
    /// </summary>
    /// <param name="pipelineId">(GitLab Global) Pipeline-ID</param>
    /// <response code="200">Success</response>
    [HttpGet]
    [Route("Get")]
    [Authorize(Policies.OAuthUser)]
    [Produces(typeof(List<PipelineFinding>))]
    public IAsyncEnumerable<PipelineFinding> GetFindings(int pipelineId)
        => _context.PipelineFindings.AsNoTracking()
            .Include(pf => pf.ToolRule)
            .Where(pf => pf.PipelineRun.PipelineId == pipelineId)
            .AsAsyncEnumerable();

    /// <summary>
    /// Returns the detailed finding by the given id
    /// </summary>
    /// <param name="findingId">ID of the finding</param>
    /// <response code="200">Success</response>
    /// <response code="404">Finding not found.</response>
    [HttpGet]
    [Route("GetDetails")]
    [Authorize(Policies.OAuthUser)]
    [Produces(typeof(PipelineFinding))]
    [ProducesErrorResponseType(typeof(string))]
    public async Task<IActionResult> GetFindingDetails(long findingId)
    {
        var finding = await _context.PipelineFindings.AsNoTracking()
            .Include(pf => pf.PipelineRun)
            .Include(pf => pf.ToolRule)
            .Include(pf => pf.Findings)
            .ThenInclude(f => f.ToolRule)
            .SingleOrDefaultAsync(pf => pf.Id == findingId);

        if (finding == null)
            return NotFound($"Finding with id {findingId} not found.");

        finding.Findings = finding.Findings.OrderBy(f => f.Tool).ToList();

        return Ok(finding);
    }

    /// <summary>
    /// Returns the latest pipeline run for the given repository and branch
    /// </summary>
    /// <param name="repository">Repository</param>
    /// <param name="branch">Branch</param>
    /// <response code="200">Found pipeline.</response>
    /// <response code="404">Pipeline not found.</response>
    [HttpGet]
    [Route("GetLatestPipelineRun")]
    [Authorize(Policies.OAuthUser)]
    [Produces(typeof(PipelineRunDto))]
    [ProducesErrorResponseType(typeof(string))]
    public async Task<IActionResult> GetLatestPipelineRunAsync(string repository, string branch)
    {
        var repo = await _context.Repositories.AsNoTracking()
            .Where(r => r.Name == repository)
            .SingleOrDefaultAsync();

        if (repo == null)
            return NotFound($"Repository \"{repository}\" not found.");

        var pipelineRun = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.RepositoryId == repo.Id && pr.Branch == branch)
            .OrderByDescending(pr => pr.PipelineId)
            .Select(pr => new PipelineRunDto
            {
                PipelineId = pr.PipelineId,
                Branch = pr.Branch,
                CommitHash = pr.CommitHash,
                CreatedAt = pr.CreatedAt,
                Repository = new RepositoryDto
                {
                    Name = pr.Repository.Name,
                    Url = pr.Repository.Url
                }
            })
            .FirstOrDefaultAsync();

        if (pipelineRun == null)
            return NotFound($"No pipeline run found for repository \"{repository}\" and branch \"{branch}\".");

        return Ok(pipelineRun);
    }

    /// <summary>
    /// Aggregates the data for the charts
    /// </summary>
    /// <param name="pipelineId">(GitLab Global) Pipeline Id</param>
    /// <response code="200">Success</response>
    /// <response code="404">Pipeline not found.</response>
    [HttpGet]
    [Route("GetChartData")]
    [Authorize(Policies.OAuthUser)]
    [Produces(typeof(ChartDataDto))]
    [ProducesErrorResponseType(typeof(string))]
    public async Task<IActionResult> GetChartDataAsync(long pipelineId)
    {
        var pipelineRun = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.PipelineId == pipelineId)
            .Select(pr => new { pr.Id, pr.RepositoryId, pr.Branch })
            .FirstOrDefaultAsync();

        if (pipelineRun == null)
            return NotFound($"Pipeline with id {pipelineId} not found.");

        var chartData = new ChartDataDto();

        var absoluteFindings = (await _context.PipelineRuns.AsNoTracking()
                .Where(pr => pr.RepositoryId == pipelineRun.RepositoryId && pr.Branch == pipelineRun.Branch)
                .OrderByDescending(pr => pr.PipelineId)
                .Select(pr => new
                {
                    pr.CreatedAt, pr.PipelineId, Count = pr.PipelineFindings.Count(pf => !pf.IsFalsePositive)
                })
                .Take(10)
                .ToListAsync())
            .OrderBy(x => x.PipelineId)
            .ToList();

        chartData.AbsoluteFindings = new ChartData
        {
            Labels = absoluteFindings.ConvertAll(pr => $"#{pr.PipelineId}: {pr.CreatedAt:dd.MM.yy HH:mm}"),
            Data = absoluteFindings.ConvertAll(pr => pr.Count)
        };

        var lastFindingLevels = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.Id == pipelineRun.Id)
            .SelectMany(pr => pr.PipelineFindings.Where(pf => !pf.IsFalsePositive))
            .Select(pf => new { pf.Level })
            .ToListAsync();

        var lastRunByLevels = lastFindingLevels.Select(l => l.Level)
            .GroupBy(l => l)
            .OrderBy(l => l.Key)
            .ToDictionary(l => l.Key, l => l.Count());

        chartData.LastRunByLevels = new ChartData()
        {
            Labels = lastRunByLevels.Keys.Select(Enum.GetName).ToList()!,
            Data = lastRunByLevels.Values.ToList()
        };

        var lastFindingRules = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.Id == pipelineRun.Id)
            .SelectMany(pr => pr.PipelineFindings.Where(pf => !pf.IsFalsePositive))
            .Select(pf => new { pf.ToolRule.Id, pf.ToolRule.Name })
            .ToListAsync();

        var sortedGroupedRules = lastFindingRules.GroupBy(r => r.Name)
            .OrderByDescending(r => r.Count())
            .ToList();

        Dictionary<string, int> lastRunByRules = new();

        if (sortedGroupedRules.Count <= 7)
        {
            lastRunByRules = sortedGroupedRules.ToDictionary(r => r.Key, r => r.Count());
        }
        else
        {
            foreach (var groupedRule in sortedGroupedRules.Take(6))
            {
                lastRunByRules.Add(groupedRule.Key, groupedRule.Count());
            }

            var remaining = sortedGroupedRules.Skip(6).Sum(r => r.Count());
            lastRunByRules.Add("Others", remaining);
        }

        chartData.LastRunByRules = new ChartData()
        {
            Labels = lastRunByRules.Keys.ToList(),
            Data = lastRunByRules.Values.ToList()
        };

        return Ok(chartData);
    }

    /// <summary>
    /// Sets the false positive status of a finding
    /// </summary>
    /// <param name="findingId">ID of the finding</param>
    /// <param name="isFalsePositive">Whether the finding is a false positive</param>
    /// <response code="200">Finding updated.</response>
    /// <response code="400">Finding not found.</response>
    [HttpPost]
    [Route("SetFalsePositive")]
    [Authorize(Policies.OAuthUser)]
    [ProducesErrorResponseType(typeof(string))]
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
