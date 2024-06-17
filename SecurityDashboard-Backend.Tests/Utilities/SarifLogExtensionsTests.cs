using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using SecurityDashboard_Backend.DomainModel;
using SecurityDashboard_Backend.Utilities;

namespace SecurityDashboard_Backend.Tests.Utilities;

public class SarifLogExtensionsTests
{
    [Fact]
    public void DetermineTool_WithEmptyLog_ShouldThrowArgumentException()
    {
        // Arrange
        var log = new SarifLog() { Runs = [] };

        // Act
        Action act = () => log.DetermineTool();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetermineTool_WithUnknownTool_ShouldReturnNull()
    {
        // Arrange
        var log = new SarifLog
        {
            Runs = [new() { Tool = new() { Driver = new() { Name = "UnknownTool" } } }]
        };

        // Act
        var result = log.DetermineTool();

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("skims", SarifTool.FluidAttacks)]
    [InlineData("Semgrep OSS", SarifTool.Semgrep)]
    [InlineData("gitleaks", SarifTool.GitLeaks)]
    [InlineData("dependency-check", SarifTool.OwaspDependencyCheck)]
    [InlineData("Trivy", SarifTool.Trivy)]
    [InlineData("ZAP", SarifTool.OwaspZap)]
    public void DetermineTool_WithKnownTool_ShouldReturnTool(string toolName, SarifTool expectedTool)
    {
        // Arrange
        var log = new SarifLog
        {
            Runs = [new() { Tool = new() { Driver = new() { Name = toolName } } }]
        };

        // Act
        var result = log.DetermineTool();

        // Assert
        result.Should().Be(expectedTool);
    }

    [Fact]
    public void GetStringRepresentation_FromWebRequest_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebRequest webRequest = null;

        // Act
        Action act = () => webRequest.GetStringRepresentation();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetStringRepresentation_FromWebRequest_WithValidData_ShouldSucceed()
    {
        // Arrange
        var webRequest = new WebRequest
        {
            Protocol = "HTTP",
            Version = "1.0",
            Method = "GET",
            Target = "http://example.com",
            Headers = new Dictionary<string, string> { ["Accept"] = "application/json" },
            Body = new ArtifactContent()
        };

        // Act
        var result = webRequest.GetStringRepresentation();

        // Assert
        result.Should().NotBeNullOrEmpty()
            .And.ContainAll("GENERAL", "HEADERS", "BODY");
    }

    [Fact]
    public void GetStringRepresentation_FromWebResponse_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebResponse webResponse = null;

        // Act
        Action act = () => webResponse.GetStringRepresentation();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetStringRepresentation_FromWebResponse_WithNoResponse_ShouldSucceed()
    {
        // Arrange
        var webResponse = new WebResponse()
        {
            NoResponseReceived = true
        };

        // Act
        var result = webResponse.GetStringRepresentation();

        // Assert
        result.Should().NotBeNullOrEmpty()
            .And.ContainAll("No", "Response");
    }

    [Fact]
    public void GetStringRepresentation_FromWebResponse_WithValidData_ShouldSucceed()
    {
        // Arrange
        var webResponse = new WebResponse()
        {
            Protocol = "HTTP",
            Version = "1.0",
            NoResponseReceived = false,
            StatusCode = 200,
            ReasonPhrase = "OK",
            Headers = new Dictionary<string, string> { ["Accept"] = "application/json" },
            Body = new ArtifactContent()
        };

        // Act
        var result = webResponse.GetStringRepresentation();

        // Assert
        result.Should().NotBeNullOrEmpty()
            .And.ContainAll("GENERAL", "HEADERS", "BODY");
    }
}
