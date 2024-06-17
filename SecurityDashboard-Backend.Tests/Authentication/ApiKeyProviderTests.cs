using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SecurityDashboard_Backend.Authentication;

namespace SecurityDashboard_Backend.Tests.Authentication;

public class ApiKeyProviderTests
{
    [Fact]
    public async Task ProvideAsync_WithValidApiKey_ReturnsApiKey()
    {
        // Arrange
        var apiKeyConfiguration = new ApiKeyConfiguration { ApiKey = "valid-api-key" };
        var apiKeyProvider = new ApiKeyProvider(Options.Create(apiKeyConfiguration));

        // Act
        var apiKey = await apiKeyProvider.ProvideAsync("valid-api-key");

        // Assert
        apiKey.Should().NotBeNull()
            .And.BeOfType<ApiKey>();
    }

    [Fact]
    public async Task ProvideAsync_WithInvalidApiKey_ReturnsNull()
    {
        // Arrange
        var apiKeyConfiguration = new ApiKeyConfiguration { ApiKey = "valid-api-key" };
        var apiKeyProvider = new ApiKeyProvider(Options.Create(apiKeyConfiguration));

        // Act
        var apiKey = await apiKeyProvider.ProvideAsync("invalid-api-key");

        // Assert
        apiKey.Should().BeNull();
    }
}
