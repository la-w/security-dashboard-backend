using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SecurityDashboard_Backend.Controllers;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Models;
using SecurityDashboard_Backend.Services;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests.Controllers;

public class SarifControllerTests : DbTestBase
{
    private SarifController _controller = null!; // Suppress warning, will be initialized in InitializeAsync
    private readonly ISarifImportService _mockedImportService = Substitute.For<ISarifImportService>();

    public SarifControllerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var logger = TestOutputHelper.CreateLogger<SarifController>();

        _controller = new SarifController(logger, Context, _mockedImportService);

        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task PostRegisterPipelineRunAsync_WithNewPipelineRunAndNewRepository_ShouldCreatePipelineRunAndRepositoryInDatabase()
    {
        // Arrange
        var pipelineRun = new PipelineRunDto
        {
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            PipelineId = 1234,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };

        // Act
        var result = await _controller.PostRegisterPipelineRunAsync(pipelineRun);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);

        var pipelineRunInDb = await Context.PipelineRuns.AsNoTracking()
            .Include(p => p.Repository)
            .SingleOrDefaultAsync(p => p.PipelineId == pipelineRun.PipelineId);

        pipelineRunInDb.Should().NotBeNull();
        pipelineRunInDb.Branch.Should().Be(pipelineRun.Branch);
        pipelineRunInDb.CommitHash.Should().Be(pipelineRun.CommitHash);
        pipelineRunInDb.CreatedAt.Should().Be(pipelineRun.CreatedAt);

        pipelineRunInDb!.Repository.Should().NotBeNull();
        pipelineRunInDb!.Repository.Name.Should().Be(pipelineRun.Repository.Name);
        pipelineRunInDb!.Repository.Url.Should().Be(pipelineRun.Repository.Url);
    }

    [Fact]
    public async Task PostRegisterPipelineRunAsync_WithNewPipelineRunAndExisitingRepository_ShouldCreatePipelineRunAndLinkRepositoryInDatabase()
    {
        // Arrange
        var repository = new Repository { Name = "some-other-name", Url = "https://git.example.com/some-name" };
        await Context.Repositories.AddAsync(repository);
        await Context.SaveChangesAsync();

        var pipelineRun = new PipelineRunDto
        {
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            PipelineId = 1234,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };

        // Act
        var result = await _controller.PostRegisterPipelineRunAsync(pipelineRun);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);

        var pipelineRunInDb = await Context.PipelineRuns.AsNoTracking()
            .Include(p => p.Repository)
            .SingleOrDefaultAsync(p => p.PipelineId == pipelineRun.PipelineId);

        pipelineRunInDb.Should().NotBeNull();
        pipelineRunInDb!.Branch.Should().Be(pipelineRun.Branch);
        pipelineRunInDb.CommitHash.Should().Be(pipelineRun.CommitHash);
        pipelineRunInDb.CreatedAt.Should().Be(pipelineRun.CreatedAt);

        pipelineRunInDb.Repository.Should().NotBeNull();
        pipelineRunInDb.Repository.Id.Should().Be(repository.Id);
        pipelineRunInDb.Repository.Name.Should().Be(pipelineRun.Repository.Name);
    }

    [Fact]
    public async Task PostRegisterPipelineRunAsync_WithExistingPipelineRun_ShouldReturnConflict()
    {
        // Arrange
        var timeStamp = DateTimeOffset.UtcNow;

        var repository = new Repository { Name = "some-other-name", Url = "https://git.example.com/some-name" };
        var pipelineRun = new PipelineRun
        {
            PipelineId = 1234,
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = timeStamp,
            Repository = repository
        };
        await Context.PipelineRuns.AddAsync(pipelineRun);
        await Context.SaveChangesAsync();

        var pipelineRunDto = new PipelineRunDto
        {
            Branch = "some-branch",
            CommitHash = "0123456789abcdef0123456789abcdef01234567",
            CreatedAt = DateTimeOffset.UtcNow,
            PipelineId = 1234,
            Repository = new() { Name = "some-name", Url = "https://git.example.com/some-name" }
        };

        // Act
        var result = await _controller.PostRegisterPipelineRunAsync(pipelineRunDto);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithEmptySarifLog_ShouldReturnBadRequest()
    {
        // Arrange - done in InitializeAsync

        // Act
        var result = await _controller.PostRegisterReportAsync(new SarifLog(), 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithSarifLogFromUnknownTool_ShouldReturnBadRequest()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "UnknownTool" } } }] };

        // Act
        var result = await _controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithSarifLogContainingMultipleRuns_ShouldReturnBadRequest()
    {
        // Arrange
        var sarifLog = new SarifLog
        {
            Runs =
            [
                new() { Tool = new() { Driver = new() { Name = "Semgrep OSS" } } },
                new() { Tool = new() { Driver = new() { Name = "skims" } } }
            ]
        };

        // Act
        var result = await _controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithServiceThrowingArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "skims" } } }] };
        _mockedImportService.ImportAsync(default, default).ThrowsAsyncForAnyArgs(new ArgumentException());


        // Act
        var result = await _controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithServiceThrowingUnhandledException_ShouldReturnInternalServerError()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "skims" } } }] };
        _mockedImportService.ImportAsync(default, default).ThrowsAsyncForAnyArgs(new NullReferenceException());


        // Act
        var result = await _controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithValidSarifLog_ShouldReturnOk()
    {
        // Arrange
        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "skims" } } }] };

        // Act
        var result = await _controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
