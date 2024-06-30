using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.Controllers;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Models;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests.Controllers;

public class FindingsControllerTests : DbTestBase
{
    private FindingsController _controller = null!; // Suppress warning, will be initialized in InitializeAsync

    public FindingsControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _controller = new FindingsController(Context);

        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1234)]
    public async Task PostSetFalsePositiveAsync_WithInvalidOrUnknownFindingId_ShouldReturnBadRequest(long findingId)
    {
        // Act
        var result = await _controller.PostSetFalsePositiveAsync(findingId, true);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostSetFalsePositiveAsync_WithValidData_ShouldUpdateFinding()
    {
        // Arrange
        var pipelineFinding = new PipelineFinding
        {
            Description = "some-description",
            IsFalsePositive = false,
            Level = FailureLevel.Warning,
            Location = "some-location",
            DetectionType = DetectionType.SCA,
            ToolRule = new()
            {
                Name = "some-rule-name",
                RuleId = "some-rule-id"
            }
        };

        var pipelineRun = new PipelineRun
        {
            Branch = "some-branch",
            CommitHash = "some-hash",
            CreatedAt = DateTime.UtcNow,
            PipelineId = 12345,
            Repository = new()
            {
                Name = "some-name",
                Url = "https://git.example.com/some-name.git"
            },
            PipelineFindings = [pipelineFinding]
        };

        await Context.PipelineRuns.AddAsync(pipelineRun);
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.PostSetFalsePositiveAsync(pipelineFinding.Id, true);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);

        var updatedPipelineFinding = await Context.PipelineFindings.AsNoTracking()
            .FirstOrDefaultAsync(pf => pf.Id == pipelineFinding.Id);
        updatedPipelineFinding.Should().NotBeNull();
        updatedPipelineFinding!.IsFalsePositive.Should().BeTrue();
    }

    [Fact]
    public async Task GetFindingsAsync_WithInvalidPipelineId_ShouldReturnNothing()
    {
        // Act
        var result = await _controller.GetFindings(50).ToListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFindingDetailsAsync_WithInvalidFindingId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetFindingDetails(50);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetFindingDetailsAsync_WithValidFindingId_ShouldReturnFinding()
    {
        // Arrange
        var pipelineFinding = new PipelineFinding
        {
            Description = "some-description",
            IsFalsePositive = false,
            Level = FailureLevel.Warning,
            Location = "some-location",
            DetectionType = DetectionType.SCA,
            ToolRule = new()
            {
                Name = "some-rule-name",
                RuleId = "some-rule-id"
            }
        };

        var pipelineRun = new PipelineRun
        {
            Branch = "some-branch",
            CommitHash = "some-hash",
            CreatedAt = DateTime.UtcNow,
            PipelineId = 12345,
            Repository = new()
            {
                Name = "some-name",
                Url = "https://git.example.com/some-name.git"
            },
            PipelineFindings = [pipelineFinding]
        };

        await Context.PipelineRuns.AddAsync(pipelineRun);
        await Context.SaveChangesAsync();


        // Act
        var result = await _controller.GetFindingDetails(pipelineFinding.Id);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>()
            .Which.Value.Should().BeOfType<PipelineFinding>();
    }

    [Theory]
    [InlineData("not-existing", "some-branch")]
    [InlineData("some-repository", "some-other-branch")]
    public async Task GetLastPipelineRunAsync_WithInvalidData_ShouldReturnNotFound(string repository, string branch)
    {
        // Arrange
        var pipelineRun = new PipelineRun()
        {
            Branch = "some-branch",
            CommitHash = "some-hash",
            CreatedAt = DateTime.UtcNow,
            PipelineId = 12345,
            Repository = new()
            {
                Name = "some-repository",
                Url = "https://git.example.com/some-repository.git"
            },
        };

        await Context.PipelineRuns.AddAsync(pipelineRun);
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLatestPipelineRunAsync(repository, branch);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetLastPipelineRunAsync_WithValidData_ShouldReturnPipelineRun()
    {
        // Arrange
        var pipelineRun = new PipelineRun()
        {
            Branch = "some-branch",
            CommitHash = "some-hash",
            CreatedAt = DateTime.UtcNow,
            PipelineId = 12345,
            Repository = new()
            {
                Name = "some-repository",
                Url = "https://git.example.com/some-repository.git"
            },
        };

        await Context.PipelineRuns.AddAsync(pipelineRun);
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.GetLatestPipelineRunAsync("some-repository", "some-branch");

        // Assert
        result.Should().BeAssignableTo<ObjectResult>()
            .Which.Value.Should().BeOfType<PipelineRunDto>();
    }

    [Fact]
    public async Task GetChartDataAsync_WithInvalidPipelineId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetChartDataAsync(50);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetChartDataAsync_WithValidPipelineId_ShouldReturnChartData()
    {
        // Arrange
        var pipelineRun = new PipelineRun()
        {
            Branch = "some-branch",
            CommitHash = "some-hash",
            CreatedAt = DateTime.UtcNow,
            PipelineId = 12345,
            Repository = new()
            {
                Name = "some-repository",
                Url = "https://git.example.com/some-repository.git"
            },
        };

        await Context.PipelineRuns.AddAsync(pipelineRun);
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.GetChartDataAsync(12345);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>()
            .Which.Value.Should().BeOfType<ChartDataDto>();
    }
}
