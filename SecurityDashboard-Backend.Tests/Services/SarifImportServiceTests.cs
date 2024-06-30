using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Services;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests.Services;

public class SarifImportServiceTests : IAsyncLifetime
{
    private SarifImportService _importService = null!; // Suppress warning, will be initialized in InitializeAsync
    private SecDashboardContext _context = null!;
    private SqliteConnection _connection = null!;
    private readonly ITestOutputHelper _testOutputHelper;

    public SarifImportServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public async Task InitializeAsync()
    {
        (_connection, _context) = await DbTestHelper.InitializeDatabaseAsync();

        var logger = _testOutputHelper.CreateLogger<SarifImportService>();

        _importService = new SarifImportService(logger, _context);
    }

    public async Task DisposeAsync()
    {
        await DbTestHelper.DisposeDatabaseAsync(_connection, _context);
    }

    private SarifLog ReadSarifTestFile(string fileName)
    {
        var filePath = Path.Combine(".", "TestFiles", $"{fileName}.sarif");
        return SarifLog.Load(filePath);
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithEmptySarifLog_ShouldThrowArgumentException()
    {
        // Act
        var action = () => _importService.ImportAsync(new SarifLog(), 1);

        // Assert
        (await action.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().BeEquivalentTo("SarifLog");
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogWithNoRuns_ShouldThrowArgumentException()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = new List<Run>() };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1);

        // Assert
        (await action.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().BeEquivalentTo("SarifLog");
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogFromUnknownTool_ShouldThrowArgumentException()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "some-tool" } } }] };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1);

        // Assert
        (await action.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().BeEquivalentTo("SarifLog");
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithValidSarifLogAndUnknownPipeline_ShouldThrowArgumentException()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "skims" } } }] };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1);

        // Assert
        (await action.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().BeEquivalentTo("PipelineId");
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogIncludingNullRule_ShouldNotFail()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "skims",
                            Rules = [new() { Id = null }]
                        }
                    }
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogIncludingNullRules_ShouldThrowArgumentException()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "skims",
                            Rules = null
                        }
                    }
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        (await action.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().BeEquivalentTo("SarifLog");
    }

    [Fact]
    public async Task ImportAsync_WhenResultsWereAlreadyImported_ShouldNotFail()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        var toolRule = new ToolRule
        {
            Name = "some-name",
            RuleId = "some-rule-id",
            Tool = SarifTool.Semgrep
        };
        await _context.ToolRules.AddAsync(toolRule);
        var finding = new SemgrepFinding()
        {
            Level = FailureLevel.Warning,
            Location = "some-location",
            PipelineRun = pipelineRun,
            ToolRule = toolRule,
            Snippet = "some-snippet",
            SnippetStartLine = 0,
            Description = "some-description",
        };
        await _context.ToolFindings.AddAsync(finding);
        await _context.SaveChangesAsync();

        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "Semgrep OSS",
                            Rules = []
                        }
                    },
                    Results = []
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogWithUnknownRuleId_ShouldNotFail()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "skims",
                            Rules =
                            [
                                new ReportingDescriptor
                                {
                                    Id = "some-rule-id",
                                    Name = "some-name",
                                    ShortDescription = new() { Text = "some-text" },
                                    FullDescription = new() { Text = "some-more-text" },
                                    HelpUri = new Uri("https://help.example.com"),
                                    Help = new() { Text = "some-help-text" }
                                }
                            ]
                        }
                    },
                    Results =
                    [
                        new Result
                        {
                            RuleId = "some-other-rule-id",
                            Level = FailureLevel.Error,
                            Message = new() { Text = "some-text" },
                            Locations = null
                        }
                    ]
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogWithNullLocation_ShouldNotFail()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "skims",
                            Rules =
                            [
                                new ReportingDescriptor
                                {
                                    Id = "some-rule-id",
                                    Name = "some-name",
                                    ShortDescription = new() { Text = "some-text" },
                                    FullDescription = new() { Text = "some-more-text" },
                                    HelpUri = new Uri("https://help.example.com"),
                                    Help = new() { Text = "some-help-text" }
                                }
                            ]
                        }
                    },
                    Results =
                    [
                        new Result
                        {
                            RuleId = "some-rule-id",
                            Level = FailureLevel.Error,
                            Message = new() { Text = "some-text" },
                            Locations = null
                        }
                    ]
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithSarifLogWithNullLocationUri_ShouldNotFail()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "skims",
                            Rules =
                            [
                                new ReportingDescriptor
                                {
                                    Id = "some-rule-id",
                                    Name = "some-name",
                                    ShortDescription = new() { Text = "some-text" },
                                    FullDescription = new() { Text = "some-more-text" },
                                    HelpUri = new Uri("https://help.example.com"),
                                    Help = new() { Text = "some-help-text" }
                                }
                            ]
                        }
                    },
                    Results =
                    [
                        new Result
                        {
                            RuleId = "some-rule-id",
                            Level = FailureLevel.Error,
                            Message = new() { Text = "some-text" },
                            Locations =
                            [
                                new()
                                {
                                    PhysicalLocation = new()
                                    {
                                        ArtifactLocation = new()
                                        {
                                            Uri = null
                                        }
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithValidSarifLog_ShouldUpdateExistingRules()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        var toolRule = new ToolRule
        {
            RuleId = "some-rule-id",
            Name = "some-name-old",
            Tool = SarifTool.FluidAttacks,
        };
        await _context.ToolRules.AddAsync(toolRule);
        await _context.SaveChangesAsync();

        var sarifRule = new ReportingDescriptor
        {
            Id = "some-rule-id",
            Name = "some-name",
            ShortDescription = new() { Text = "some-text" },
            FullDescription = new() { Text = "some-more-text" },
            HelpUri = new Uri("https://help.example.com"),
            Help = new() { Text = "some-help-text" }
        };
        var sarifLog = new SarifLog
        {
            Runs =
            [
                new()
                {
                    Tool = new()
                    {
                        Driver = new()
                        {
                            Name = "skims", Rules = [sarifRule]
                        }
                    }
                }
            ]
        };

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
        toolRule = await _context.ToolRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == toolRule.Id);
        toolRule.Should().NotBeNull();
        toolRule!.Name.Should().Be(sarifRule.Name);
        toolRule.ShortDescription.Should().Be(sarifRule.ShortDescription.Text);
        toolRule.FullDescription.Should().Be(sarifRule.FullDescription.Text);
        toolRule.DefaultLevel.Should().BeNull();
        toolRule.HelpText.Should().Be(sarifRule.Help.Text);
        toolRule.HelpUri.Should().NotBeNull()
            .And.Match(x => new Uri(x) == sarifRule.HelpUri);
    }

    [Theory]
    [InlineData("FluidAttacks-SAST", 162, 110, SarifTool.FluidAttacks)]
    [InlineData("FluidAttacks-DAST", 162, 181, SarifTool.FluidAttacks)]
    [InlineData("Gitleaks", 171, 126, SarifTool.GitLeaks)]
    [InlineData("OwaspDependencyCheck", 28, 28, SarifTool.OwaspDependencyCheck)]
    [InlineData("Semgrep", 1039, 53, SarifTool.Semgrep)]
    [InlineData("Trivy", 61, 73, SarifTool.Trivy)]
    [InlineData("OwaspZap", 18, 343, SarifTool.OwaspZap)]
    public async Task ImportAsync_WhenCalledWithValidSarifLog_ShouldImportRulesAndFindings(string fileName, int ruleCount, int resultsCount,
        SarifTool sarifTool)
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = ReadSarifTestFile(fileName);


        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
        (await _context.ToolRules.AsNoTracking().Where(r => r.Tool == sarifTool).CountAsync())
            .Should().Be(ruleCount);
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == sarifTool && f.PipelineRun.PipelineId == 1234).CountAsync())
            .Should().Be(resultsCount);
    }

    [Theory]
    [InlineData("FluidAttacks-SAST", false)]
    [InlineData("FluidAttacks-DAST", true)]
    public async Task ImportAsync_WhenCalledWithFluidAttacksSarifLog_ShouldFillResultDetails(string fileName, bool isDast)
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = ReadSarifTestFile(fileName);


        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234)
            .OfType<FluidAttacksFinding>()
            .AnyAsync(f => f.Cvss31Vector != null)).Should().BeTrue();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234)
            .OfType<FluidAttacksFinding>()
            .AnyAsync(f => f.Cvss40Vector != null)).Should().BeTrue();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234)
            .OfType<FluidAttacksFinding>()
            .AnyAsync(f => f.CweIds.Count > 0)).Should().BeTrue();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234)
            .AllAsync(f => f.Level != FailureLevel.None)).Should().BeTrue();

        if (!isDast)
        {
            (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234)
                .OfType<FluidAttacksFinding>()
                .AnyAsync(f => f.SecurityAdvisories.Count > 0)).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("FluidAttacks-SAST")]
    [InlineData("FluidAttacks-DAST")]
    public async Task ImportAsync_WhenCalledWithFluidAttacksSarifLog_ShouldNotImportSameLogTwice(string fileName)
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = ReadSarifTestFile(fileName);


        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);


        // Assert
        await action.Should().NotThrowAsync();
        var previousCount = await _context.ToolFindings.AsNoTracking()
            .Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234).CountAsync();
        await action.Should().NotThrowAsync();
        var newCount = await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.FluidAttacks && f.PipelineRun.PipelineId == 1234)
            .CountAsync();
        newCount.Should().Be(previousCount);
    }


    [Fact]
    public async Task ImportAsync_WhenCalledWithTrivySarifLog_ShouldFillResultDetails()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = ReadSarifTestFile("Trivy");


        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.Trivy && f.PipelineRun.PipelineId == 1234)
            .OfType<TrivyFinding>()
            .AnyAsync(f => f.SecurityAdvisories.Count > 0)).Should().BeTrue();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithOwaspDependencyCheckSarifLog_ShouldFillResultDetails()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = ReadSarifTestFile("OwaspDependencyCheck");


        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
        (await _context.ToolRules.AsNoTracking()
                .Where(r => r.Tool == SarifTool.OwaspDependencyCheck)
                .AnyAsync(r => !string.IsNullOrWhiteSpace(r.CvssVector)))
            .Should().BeTrue();
        (await _context.ToolFindings.AsNoTracking()
                .Where(f => f.Tool == SarifTool.OwaspDependencyCheck)
                .AnyAsync(f => f.SecurityAdvisories.Count > 0))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ImportAsync_WhenCalledWithOwaspZapSarifLog_ShouldFillResultDetails()
    {
        // Arrange
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            Repository = new Repository { Name = "some-name", Url = "https://git.example.com/some-name" }
        };
        await _context.PipelineRuns.AddAsync(pipelineRun);
        await _context.SaveChangesAsync();

        var sarifLog = ReadSarifTestFile("OwaspZap");

        // Act
        var action = () => _importService.ImportAsync(sarifLog, 1234);

        // Assert
        await action.Should().NotThrowAsync();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.OwaspZap && f.PipelineRun.PipelineId == 1234)
            .OfType<OwaspZapFinding>()
            .AnyAsync(f => !string.IsNullOrEmpty(f.Attack))).Should().BeTrue();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.OwaspZap && f.PipelineRun.PipelineId == 1234)
            .OfType<OwaspZapFinding>()
            .AnyAsync(f => !string.IsNullOrEmpty(f.WebRequest))).Should().BeTrue();
        (await _context.ToolFindings.AsNoTracking().Where(f => f.Tool == SarifTool.OwaspZap && f.PipelineRun.PipelineId == 1234)
            .OfType<OwaspZapFinding>()
            .AnyAsync(f => !string.IsNullOrEmpty(f.WebResponse))).Should().BeTrue();
    }
}
