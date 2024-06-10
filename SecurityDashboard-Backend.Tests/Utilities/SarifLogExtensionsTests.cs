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
}
