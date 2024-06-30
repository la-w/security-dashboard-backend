using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Services;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests.Services;

public class FindingsDeduplicationServiceTests : IAsyncLifetime
{
    private FindingsDeduplicationService _deduplicationService = null!; // Suppress warning, will be initialized in InitializeAsync
    private SecDashboardContext _context = null!;
    private SqliteConnection _connection = null!;
    private readonly ITestOutputHelper _testOutputHelper;

    public FindingsDeduplicationServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public async Task InitializeAsync()
    {
        (_connection, _context) = await DbTestHelper.InitializeDatabaseAsync();

        var logger = _testOutputHelper.CreateLogger<FindingsDeduplicationService>();

        _deduplicationService = new FindingsDeduplicationService(logger, _context);
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

    private async Task ImportAllTestFiles(long pipelineId = 1234)
    {
        var importService = new SarifImportService(Substitute.For<ILogger<SarifImportService>>(), _context);

        var repository = await _context.Repositories.FirstOrDefaultAsync();

        if (repository == null)
        {
            repository = new Repository
            {
                Name = "some-name",
                Url = "https://git.example.com/some-name"
            };
            await _context.Repositories.AddAsync(repository);
        }

        await _context.PipelineRuns.AddAsync(
            new PipelineRun()
            {
                Branch = "some-branch",
                CommitHash = "0123456789012345678901234567890123456789",
                CreatedAt = DateTimeOffset.UtcNow,
                PipelineId = pipelineId,
                Repository = repository
            });

        await _context.SaveChangesAsync();

        var testFilesPath = Path.Combine(".", "TestFiles");
        foreach (var file in Directory.GetFiles(testFilesPath))
        {
            var log = SarifLog.Load(file);
            await importService.ImportAsync(log, pipelineId);
        }
    }

    [Fact]
    public async Task DeduplicateFindingsAsync_WithUnknownPipelineId_ShouldThrowArgumentException()
    {
        // Arrange

        // Act
        var action = () => _deduplicationService.DeduplicateFindingsAsync(0);

        // Assert
        (await action.Should().ThrowAsync<ArgumentException>())
            .Which.ParamName.Should().BeEquivalentTo("PipelineId");
    }

    [Fact]
    public async Task DeduplicateFindingsAsync_WithKnownPipelineId_ShouldJustWork()
    {
        // Arrange
        await ImportAllTestFiles();

        // Act
        var action = () => _deduplicationService.DeduplicateFindingsAsync(1234);

        // Assert
        await action.Should().NotThrowAsync();
        var pipelineFindingsCount = await _context.PipelineFindings.AsNoTracking().CountAsync();
        var toolFindingsCount = await _context.ToolFindings.AsNoTracking().CountAsync();
        pipelineFindingsCount.Should().BeGreaterThan(0)
            .And.BeLessThan(toolFindingsCount);
    }

    [Fact]
    public async Task DeduplicateFindingsAsync_WithAlreadyDeduplicatedPipeline_ShouldThrowArgumentException()
    {
        // Arrange
        await ImportAllTestFiles();

        // Act
        var action = () => _deduplicationService.DeduplicateFindingsAsync(1234);

        // Assert
        await action.Should().NotThrowAsync();
        (await action.Should().ThrowAsync<ArgumentException>())
            .And.ParamName.Should().BeEquivalentTo("PipelineId");
    }

    [Fact]
    public async Task DeduplicateFindingAsync_WithFalsePositivesSet_ShouldCopyFalsePositives()
    {
        // Arrange
        await ImportAllTestFiles(1000);
        await _deduplicationService.DeduplicateFindingsAsync(1000);
        await ImportAllTestFiles(2000);

        var falsePositiveCount = await _context.PipelineFindings
            .Where(pf => pf.PipelineRun.PipelineId == 1000 && pf.DetectionType == DetectionType.SCA)
            .ExecuteUpdateAsync(setters => setters.SetProperty(pf => pf.IsFalsePositive, true));

        // Act
        var action = () => _deduplicationService.DeduplicateFindingsAsync(2000);

        // Assert
        await action.Should().NotThrowAsync();
        var newFalsePositiveCount = await _context.PipelineFindings.Where(pf => pf.PipelineRun.PipelineId == 2000 && pf.IsFalsePositive).CountAsync();
        newFalsePositiveCount.Should().BeInRange(falsePositiveCount / 2, falsePositiveCount);
    }
}
