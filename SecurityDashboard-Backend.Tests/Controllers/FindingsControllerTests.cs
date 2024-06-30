using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.EntityFrameworkCore;
using SecurityDashboard_Backend.Controllers;
using SecurityDashboard_Backend.DomainModel;
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
}
