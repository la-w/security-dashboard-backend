using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.DomainModel;

namespace SecurityDashboard_Backend.Services;

public class FindingsDeduplicationService : IFindingsDeduplicationService
{
    private readonly ILogger<FindingsDeduplicationService> _logger;
    private readonly SecDashboardContext _context;

    private static readonly Regex TrivyDescriptionPackageNameRegex = new Regex("Package: ([^\\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public FindingsDeduplicationService(ILogger<FindingsDeduplicationService> logger, SecDashboardContext context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task DeduplicateFindingsAsync(long pipelineId)
    {
        var pipelineRun = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.PipelineId == pipelineId)
            .Select(pr => new
            {
                PipelineRunId = pr.Id, FindingsCount = pr.PipelineFindings.Count, ToolFindingsCount = pr.ToolFindings.Count, pr.RepositoryId,
                pr.Branch
            })
            .SingleOrDefaultAsync();

        if (pipelineRun == null)
            throw new ArgumentException("Pipeline not found", nameof(pipelineId));

        if (pipelineRun.FindingsCount > 0)
            throw new ArgumentException("Pipeline was already deduplicated.", nameof(pipelineId));

        if (pipelineRun.ToolFindingsCount == 0)
            throw new ArgumentException("No findings to deduplicate.", nameof(pipelineId));

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await DeduplicateSastAsync(pipelineRun.PipelineRunId);
            var timeSast = stopwatch.Elapsed;
            _logger.LogInformation("Deduplicated SAST findings in {Time}", timeSast);

            await DeduplicateScaAsync(pipelineRun.PipelineRunId);
            var timeSca = stopwatch.Elapsed - timeSast;
            _logger.LogInformation("Deduplicated SCA findings in {Time}", timeSca);

            await DeduplicateDastAsync(pipelineRun.PipelineRunId);
            var timeDast = stopwatch.Elapsed - timeSast - timeSca;
            _logger.LogInformation("Deduplicated DAST findings in {Time}", timeDast);

            await DeduplicateSecretAsync(pipelineRun.PipelineRunId);
            var timeSecret = stopwatch.Elapsed - timeSast - timeSca - timeDast;
            _logger.LogInformation("Deduplicated SECRET findings in {Time}", timeSecret);

            await CreateMissingPipelineFindingsAsync(pipelineRun.PipelineRunId);
            var timeMissing = stopwatch.Elapsed - timeSast - timeSca - timeDast - timeSecret;
            _logger.LogInformation("Created missing pipeline findings in {Time}", timeMissing);

            await CopyFalsePositiveStateAsync(pipelineRun.PipelineRunId, pipelineRun.RepositoryId, pipelineRun.Branch);
            var timeFalsePositive = stopwatch.Elapsed - timeSast - timeSca - timeDast - timeSecret - timeMissing;
            _logger.LogInformation("Copied false positive state in {Time}", timeFalsePositive);

            await transaction.CommitAsync();

            stopwatch.Stop();
            var totalTime = stopwatch.Elapsed;

            var deduplicatedFindingsCount = await _context.PipelineFindings.AsNoTracking()
                .Where(pf => pf.PipelineRunId == pipelineRun.PipelineRunId)
                .CountAsync();
            var totalFindingsCount = await _context.ToolFindings.AsNoTracking()
                .Where(tf => tf.PipelineRunId == pipelineRun.PipelineRunId)
                .CountAsync();

            var deduplicationRate = (decimal)deduplicatedFindingsCount / totalFindingsCount;

            _logger.LogInformation(
                "{TotalFindings} findings have been deduplicated to {DeduplicatedFindings} PipelineFindings (Rate: {DeduplicationRate:P2}, Time needed: {TotalTime})",
                totalFindingsCount, deduplicatedFindingsCount, deduplicationRate, totalTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deduplicating findings");
            await transaction.RollbackAsync();
        }
    }

    private async Task DeduplicateSastAsync(long pipelineRunId)
    {
        var toolFindings = await _context.ToolFindings
            .OrderBy(tf => tf.Tool)
            .Where(tf => tf.DetectionType == DetectionType.SAST && tf.PipelineRunId == pipelineRunId)
            .Select(tf => new
            {
                tf.Location,
                Line = tf.Tool == SarifTool.FluidAttacks ? ((FluidAttacksFinding)tf).Line ?? tf.SnippetStartLine : tf.SnippetStartLine,
                Finding = tf
            })
            .ToListAsync();

        var locationGroupedFindings = toolFindings.GroupBy(tf => new { tf.Location, tf.Line }, tf => tf.Finding)
            .ToDictionary(g => g.Key);

        var pipelineFindings = new List<PipelineFinding>();

        foreach (var (key, groupedFindings) in locationGroupedFindings)
        {
            var pipelineFinding = new PipelineFinding
            {
                Level = groupedFindings.Max(g => g.Level),
                PipelineRunId = pipelineRunId,
                CweIds = groupedFindings.SelectMany(g => g.CweIds).Distinct().ToList(),
                SecurityAdvisories = groupedFindings.SelectMany(g => g.SecurityAdvisories).Distinct().ToList(),
                DetectionType = DetectionType.SAST,
                Description = groupedFindings.MinBy(f => f.Tool)!.Description,
                Findings = groupedFindings.ToList(),
                Location = key.Location,
                ToolRuleId = groupedFindings.MinBy(f => f.Tool)!.ToolRuleId
            };

            pipelineFindings.Add(pipelineFinding);
        }

        await _context.PipelineFindings.AddRangeAsync(pipelineFindings);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var pipelineFinding in pipelineFindings)
                _logger.LogDebug("Deduplicated {FindingsCount} SAST findings to one pipeline finding: {Location}",
                    pipelineFinding.Findings.Distinct().Count(), pipelineFinding.Location);
        }

        await _context.SaveChangesAsync();
    }

    private async Task DeduplicateScaAsync(long pipelineRunId)
    {
        var toolFindings = await _context.ToolFindings
            .Where(tf => tf.DetectionType == DetectionType.SCA && tf.PipelineRunId == pipelineRunId)
            .ToListAsync();

        var allSecurityAdvisories = toolFindings
            .SelectMany(f => f.SecurityAdvisories)
            .Distinct();

        var securityAdvisoryGrouping = new Dictionary<string, List<ToolFinding>>();

        foreach (var toolFinding in toolFindings.Where(f => f.DetectionType == DetectionType.SCA && f.SecurityAdvisories.Count > 0))
        {
            foreach (var upperSecurityAdvisory in toolFinding.SecurityAdvisories)
            {
                if (securityAdvisoryGrouping.TryGetValue(upperSecurityAdvisory, out var findings))
                {
                    findings.Add(toolFinding);
                }
                else
                {
                    securityAdvisoryGrouping[upperSecurityAdvisory] = [toolFinding];
                }
            }
        }

        var pipelineFindings = new List<PipelineFinding>();

        foreach (var securityAdvisory in allSecurityAdvisories)
        {
            if (!securityAdvisoryGrouping.TryGetValue(securityAdvisory, out var findings))
                continue;

            var checkedAdvisories = new List<string>();
            var uncheckedAdvisories = new List<string>() { securityAdvisory };
            var allFindings = new List<ToolFinding>();

            while (uncheckedAdvisories.Count >= 1)
            {
                var advisory = uncheckedAdvisories[0];
                uncheckedAdvisories.RemoveAt(0);

                if (checkedAdvisories.Contains(advisory))
                    continue;

                checkedAdvisories.Add(advisory);

                if (!securityAdvisoryGrouping.TryGetValue(advisory, out var newFindings))
                    continue;

                uncheckedAdvisories.AddRange(newFindings.SelectMany(x => x.SecurityAdvisories));

                allFindings.AddRange(newFindings);
            }

            var uniqueFindings = allFindings.Distinct().ToList();

            var pipelineFinding = new PipelineFinding
            {
                DetectionType = DetectionType.SCA,
                SecurityAdvisories = checkedAdvisories,
                CweIds = uniqueFindings.SelectMany(f => f.CweIds).Distinct().ToList(),
                Findings = uniqueFindings,
                Level = uniqueFindings.Max(x => x.Level),
                Description = uniqueFindings.MinBy(f => f.Tool).Description,
                Location = uniqueFindings.MinBy(f => f.Tool).Location,
                PipelineRunId = pipelineRunId,
                ToolRuleId = uniqueFindings.MinBy(f => f.Tool)!.ToolRuleId
            };

            pipelineFindings.Add(pipelineFinding);

            foreach (var advisory in checkedAdvisories)
            {
                securityAdvisoryGrouping.Remove(advisory);
            }
        }

        var singleTrivyFindings = pipelineFindings.Where(pf => pf.Findings.Count == 1 && pf.Findings[0].Tool == SarifTool.Trivy)
            .GroupBy(f =>
            {
                var match = TrivyDescriptionPackageNameRegex.Match(f.Description);
                if (match.Success)
                    return match.Groups[1].Value;
                return null;
            })
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key, g => g);

        foreach (var (packageName, findings) in singleTrivyFindings)
        {
            if (findings.Count() == 1)
                continue;

            var findingToUpdate = findings.First();

            foreach (var outdatedFinding in findings.Skip(1))
                pipelineFindings.Remove(outdatedFinding);

            findingToUpdate.SecurityAdvisories = findings.SelectMany(f => f.SecurityAdvisories).Distinct().ToList();
            findingToUpdate.Level = findings.Max(f => f.Level);
            findingToUpdate.Findings.AddRange(findings.Skip(1).SelectMany(f => f.Findings));
        }

        await _context.PipelineFindings.AddRangeAsync(pipelineFindings);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var pipelineFinding in pipelineFindings)
                _logger.LogDebug("Deduplicated {FindingsCount} SCA findings to one pipeline finding: {Advisories}",
                    pipelineFinding.Findings.Distinct().Count(), pipelineFinding.SecurityAdvisories);
        }

        await _context.SaveChangesAsync();
    }

    private async Task DeduplicateDastAsync(long pipelineRunId)
    {
        var toolFindings = await _context.ToolFindings
            .Where(tf => tf.PipelineRunId == pipelineRunId && tf.DetectionType == DetectionType.DAST)
            .ToListAsync();

        var ruleGroupedFindings = toolFindings.GroupBy(f => f.ToolRuleId).ToDictionary(g => g.Key);

        var pipelineFindings = new List<PipelineFinding>();

        foreach (var (ruleId, groupedFindings) in ruleGroupedFindings)
        {
            if (groupedFindings.Count() > 10)
            {
                // General problem
                var pipelineFinding = new PipelineFinding
                {
                    Description = groupedFindings.MinBy(f => f.Tool).Description,
                    CweIds = groupedFindings.SelectMany(f => f.CweIds).Distinct().ToList(),
                    SecurityAdvisories = groupedFindings.SelectMany(f => f.SecurityAdvisories).Distinct().ToList(),
                    DetectionType = DetectionType.DAST,
                    Level = groupedFindings.Max(f => f.Level),
                    Location = groupedFindings.MinBy(f => f.Tool).Location,
                    Findings = groupedFindings.ToList(),
                    PipelineRunId = pipelineRunId,
                    ToolRuleId = groupedFindings.MinBy(f => f.Tool)!.ToolRuleId
                };

                pipelineFindings.Add(pipelineFinding);
            }
            else
            {
                var locationGroupedFindings = groupedFindings.GroupBy(f => f.Location).ToDictionary(g => g.Key);

                foreach (var (location, locationFindings) in locationGroupedFindings)
                {
                    if (locationFindings.Count() > 1)
                    {
                        // Problem with same location
                        var pipelineFinding = new PipelineFinding
                        {
                            Description = locationFindings.MinBy(f => f.Tool).Description,
                            CweIds = locationFindings.SelectMany(f => f.CweIds).Distinct().Order().ToList(),
                            SecurityAdvisories = locationFindings.SelectMany(f => f.SecurityAdvisories).Distinct().Order().ToList(),
                            DetectionType = DetectionType.DAST,
                            Level = locationFindings.Max(f => f.Level),
                            Location = location,
                            Findings = locationFindings.ToList(),
                            PipelineRunId = pipelineRunId,
                            ToolRuleId = locationFindings.MinBy(f => f.Tool)!.ToolRuleId
                        };

                        pipelineFindings.Add(pipelineFinding);
                    }
                    else
                    {
                        // No problem
                        var pipelineFinding = new PipelineFinding
                        {
                            Description = locationFindings.First().Description,
                            CweIds = locationFindings.First().CweIds,
                            SecurityAdvisories = locationFindings.First().SecurityAdvisories,
                            DetectionType = DetectionType.DAST,
                            Level = locationFindings.First().Level,
                            Location = location,
                            Findings = locationFindings.ToList(),
                            PipelineRunId = pipelineRunId,
                            ToolRuleId = locationFindings.MinBy(f => f.Tool)!.ToolRuleId
                        };

                        pipelineFindings.Add(pipelineFinding);
                    }
                }
            }
        }

        await _context.PipelineFindings.AddRangeAsync(pipelineFindings);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var pipelineFinding in pipelineFindings)
                _logger.LogDebug("Deduplicated {FindingsCount} DAST findings to one pipeline finding.", pipelineFinding.Findings.Distinct().Count());
        }

        await _context.SaveChangesAsync();
    }

    private async Task DeduplicateSecretAsync(long pipelineRunId)
    {
        var secretFindings = await _context.ToolFindings
            .Where(tf => tf.PipelineRunId == pipelineRunId && tf.DetectionType == DetectionType.SECRET)
            .ToListAsync();

        var groupedSecretFindings = secretFindings.GroupBy(f => new { f.Location, f.Snippet }).ToDictionary(g => g.Key);

        var pipelineFindings = new List<PipelineFinding>();

        foreach (var (key, groupedFindings) in groupedSecretFindings)
        {
            var pipelineFinding = new PipelineFinding
            {
                Level = groupedFindings.Max(g => g.Level),
                PipelineRunId = pipelineRunId,
                CweIds = groupedFindings.SelectMany(g => g.CweIds).Distinct().ToList(),
                SecurityAdvisories = groupedFindings.SelectMany(g => g.SecurityAdvisories).Distinct().ToList(),
                DetectionType = DetectionType.SECRET,
                Description = groupedFindings.MinBy(f => f.Tool)!.Description,
                Findings = groupedFindings.ToList(),
                Location = key.Location,
                ToolRuleId = groupedFindings.MinBy(f => f.Tool)!.ToolRuleId
            };

            pipelineFindings.Add(pipelineFinding);
        }

        await _context.PipelineFindings.AddRangeAsync(pipelineFindings);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var pipelineFinding in pipelineFindings)
                _logger.LogDebug("Deduplicated {FindingsCount} SECRET findings to one pipeline finding: {Location}",
                    pipelineFinding.Findings.Distinct().Count(), pipelineFinding.Location);
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateMissingPipelineFindingsAsync(long pipelineRunId)
    {
        var missingFindings = await _context.ToolFindings
            .Where(tf => tf.PipelineRunId == pipelineRunId && tf.PipelineFindingId == null)
            .ToListAsync();

        foreach (var missingFinding in missingFindings)
        {
            var pipelineFinding = new PipelineFinding
            {
                CweIds = missingFinding.CweIds,
                Description = missingFinding.Description,
                DetectionType = missingFinding.DetectionType,
                Findings = [missingFinding],
                Level = missingFinding.Level,
                Location = missingFinding.Location,
                PipelineRunId = pipelineRunId,
                ToolRuleId = missingFinding.ToolRuleId
            };

            await _context.PipelineFindings.AddAsync(pipelineFinding);
        }

        await _context.SaveChangesAsync();
    }

    private async Task CopyFalsePositiveStateAsync(long pipelineRunId, int repositoryId, string branch)
    {
        var previousPipelineRun = await _context.PipelineRuns.AsNoTracking()
            .Where(pr => pr.Id != pipelineRunId && pr.RepositoryId == repositoryId && pr.Branch == branch)
            .OrderByDescending(pr => pr.PipelineId)
            .Select(pr => new { pr.Id })
            .FirstOrDefaultAsync();

        if (previousPipelineRun == null)
        {
            _logger.LogInformation("Cannot copy false positive state, no previous pipeline run found.");
            return;
        }

        var currentPipelineFindings = await _context.PipelineFindings
            .Where(pf => pf.PipelineRunId == pipelineRunId)
            .ToListAsync();
        var previousPipelineFindings = await _context.PipelineFindings.AsNoTracking()
            .Where(pf => pf.PipelineRunId == previousPipelineRun.Id)
            .ToListAsync();

        var copiedFalsePositives = 0;

        foreach (var currentPipelineFinding in currentPipelineFindings)
        {
            try
            {
                var previousFinding = previousPipelineFindings.SingleOrDefault(pf => pf.ToolRuleId == currentPipelineFinding.ToolRuleId
                                                                                     && pf.Location == currentPipelineFinding.Location
                                                                                     && pf.DetectionType == currentPipelineFinding.DetectionType
                                                                                     && pf.Level == currentPipelineFinding.Level);

                if (previousFinding == null)
                    continue;

                currentPipelineFinding.IsFalsePositive = previousFinding.IsFalsePositive;
                copiedFalsePositives++;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug("More than one previous finding matches finding #{FindingId}", currentPipelineFinding.Id);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully copied false positive state for {CopiedFalsePositives} findings.", copiedFalsePositives);
    }
}
