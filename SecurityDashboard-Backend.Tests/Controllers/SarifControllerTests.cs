using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SecurityDashboard_Backend.Controllers;

namespace SecurityDashboard_Backend.Tests.Controllers;

public class SarifControllerTests
{
    [Fact]
    public async Task PostRegisterReportAsync_WithEmptySarifLog_ShouldReturnBadRequest()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SarifController>>();
        var httpContext = new DefaultHttpContext();
        var controller = new SarifController(logger);

        controller.ControllerContext.HttpContext = httpContext;

        // Act
        var result = await controller.PostRegisterReportAsync(new SarifLog(), 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithSarifLogFromUnknownTool_ShouldReturnBadRequest()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SarifController>>();
        var httpContext = new DefaultHttpContext();
        var controller = new SarifController(logger);

        controller.ControllerContext.HttpContext = httpContext;

        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "UnknownTool" } } }] };

        // Act
        var result = await controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task PostRegisterReportAsync_WithValidSarifLog_ShouldReturnOk()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SarifController>>();
        var httpContext = new DefaultHttpContext();
        var controller = new SarifController(logger);

        controller.ControllerContext.HttpContext = httpContext;

        var sarifLog = new SarifLog { Runs = [new() { Tool = new() { Driver = new() { Name = "skims" } } }] };

        // Act
        var result = await controller.PostRegisterReportAsync(sarifLog, 1);

        // Assert
        result.Should().NotBeNull()
            .And.BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
