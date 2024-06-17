using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.CodeAnalysis.Sarif;
using NSubstitute;
using SecurityDashboard_Backend.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SecurityDashboard_Backend.Tests.Utilities;

public class SarifInputFormatterTests
{
    [ExcludeFromCodeCoverage(Justification = "Test Helper method")]
    private InputFormatterContext GetMockedInputFormatterContext()
    {
        var httpContext = new DefaultHttpContext();
        var inputFormatterContext = new InputFormatterContext(httpContext,
            string.Empty,
            new ModelStateDictionary(),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(SarifLog)),
            (stream, encoding) => new StreamReader(stream, encoding));

        return inputFormatterContext;
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithEmptyStream_ShouldReturnNull()
    {
        // Arrange
        var formatter = new SarifInputFormatter();
        var context = GetMockedInputFormatterContext();

        await using var stream = new MemoryStream();
        context.HttpContext.Request.Body = stream;

        // Act
        var result = await formatter.ReadRequestBodyAsync(context, Encoding.Default);

        // Assert
        result.HasError.Should().BeFalse();
        result.Model.Should().BeNull();
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithNullStream_ShouldReturnError()
    {
        // Arrange
        var formatter = new SarifInputFormatter();
        var context = GetMockedInputFormatterContext();

        context.HttpContext.Request.Body = null!;

        // Act
        var result = await formatter.ReadRequestBodyAsync(context, Encoding.Default);

        // Assert
        result.HasError.Should().BeTrue();
    }

    [Theory]
    [InlineData("Invalid JSON")]
    [InlineData("{}")]
    [InlineData("{\"Runs\": []}")]
    public async Task ReadRequestBodyAsync_WithInvalidStream_ShouldReturnError(string input)
    {
        // Arrange
        var formatter = new SarifInputFormatter();
        var context = GetMockedInputFormatterContext();


        await using var stream = new MemoryStream(Encoding.Default.GetBytes(input));
        context.HttpContext.Request.Body = stream;

        // Act
        var result = await formatter.ReadRequestBodyAsync(context, Encoding.Default);

        // Assert
        result.HasError.Should().BeTrue();
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithValidStream_ShouldReturnSarifLog()
    {
        // Arrange
        var formatter = new SarifInputFormatter();
        var context = GetMockedInputFormatterContext();

        var sarifLog = new SarifLog();
        await using var stream = new MemoryStream();
        sarifLog.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);

        context.HttpContext.Request.Body = stream;

        // Act
        var result = await formatter.ReadRequestBodyAsync(context, Encoding.Default);

        // Assert
        result.HasError.Should().BeFalse();
        result.Model.Should().BeEquivalentTo(sarifLog);
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithDisabledAllowedSynchronizedIO_ShouldNotKeepAllowSynchronizedIOEnabled()
    {
        // Arrange
        var formatter = new SarifInputFormatter();
        var context = GetMockedInputFormatterContext();

        var syncIoFeature = Substitute.For<IHttpBodyControlFeature>();
        syncIoFeature.AllowSynchronousIO = false;

        context.HttpContext.Features.Set(syncIoFeature);

        // Act
        await formatter.ReadRequestBodyAsync(context, Encoding.Default);

        // Assert
        syncIoFeature.AllowSynchronousIO.Should().BeFalse();
    }
}
