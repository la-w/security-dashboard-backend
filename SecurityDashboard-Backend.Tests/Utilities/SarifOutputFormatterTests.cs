using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.CodeAnalysis.Sarif;
using NSubstitute;
using SecurityDashboard_Backend.Utilities;
using System.Text;

namespace SecurityDashboard_Backend.Tests.Utilities;

public class SarifOutputFormatterTests
{
    private OutputFormatterWriteContext GetMockedOutputFormatterWriteContext(SarifLog? logToWrite)
    {
        var httpContext = new DefaultHttpContext();
        var outputFormatterWriteContext = new OutputFormatterWriteContext(
            httpContext, (stream, encoding) => new StreamWriter(stream, encoding), typeof(SarifLog), logToWrite);

        return outputFormatterWriteContext;
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithNullLog_ShouldNotWriteAnything()
    {
        // Arrange
        var formatter = new SarifOutputFormatter();
        var context = GetMockedOutputFormatterWriteContext(null);

        await using var stream = new MemoryStream();
        context.HttpContext.Response.Body = stream;

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.Default);

        // Assert
        context.HttpContext.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithValidLog_ShouldWriteLogToResponseBody()
    {
        // Arrange
        var formatter = new SarifOutputFormatter();
        var context = GetMockedOutputFormatterWriteContext(new SarifLog());
        await using var stream = new MemoryStream();
        context.HttpContext.Response.Body = stream;

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.Default);

        // Assert
        context.HttpContext.Response.Body.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithDisabledAllowedSynchronizedIO_ShouldNotKeepAllowSynchronizedIOEnabled()
    {
        // Arrange
        var formatter = new SarifOutputFormatter();
        var context = GetMockedOutputFormatterWriteContext(new SarifLog());

        var syncIoFeature = Substitute.For<IHttpBodyControlFeature>();
        syncIoFeature.AllowSynchronousIO = false;

        context.HttpContext.Features.Set(syncIoFeature);

        // Act
        await formatter.WriteResponseBodyAsync(context, Encoding.Default);

        // Assert
        syncIoFeature.AllowSynchronousIO.Should().BeFalse();
    }
}
