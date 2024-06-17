using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using SecurityDashboard_Backend.Utilities;
using System.Net;
using Xunit.Abstractions;

namespace SecurityDashboard_Backend.Tests.Utilities;

public class RemoteIpCheckActionFilterTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RemoteIpCheckActionFilterTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private ActionExecutingContext GetMockedActionExecutingContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(),
            Substitute.For<ControllerBase>());

        return actionExecutingContext;
    }

    [Fact]
    public void OnActionExecuting_WithWhitelistSetToNull_ShouldNotSetResult()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter(null, logger);

        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_WithEmptyWhitelist_ShouldNotSetResult()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter([], logger);

        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_WithNullRemoteIp_ShouldReturnStatusCode403()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter(["127.0.0.1", "::1"], logger);

        context.HttpContext.Connection.RemoteIpAddress = null;

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void OnActionExecuting_WithWhitelistedRemoteIp_ShouldNotSetResult()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter(["127.0.0.1", "::1"], logger);

        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        // Act 
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_WithNonWhitelistedRemoteIp_ShouldReturnStatusCode403()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter(["127.0.0.1", "::1"], logger);

        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("1.0.0.1");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void OnActionExecuting_WithWhitelistedRemoteIpV4MappedToV6_ShouldNotSetResult()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter(["8.8.8.8"], logger);

        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:808:808");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact]
    public void OnActionExecuting_WithNonWhitelistedRemoteIpV4MappedToV6_ShouldReturnStatusCode403()
    {
        // Arrange
        var logger = _testOutputHelper.CreateLogger<RemoteIpCheckActionFilter>();
        var context = GetMockedActionExecutingContext();
        var filter = new RemoteIpCheckActionFilter(["127.0.0.1"], logger);

        context.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:808:808");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        context.Result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
