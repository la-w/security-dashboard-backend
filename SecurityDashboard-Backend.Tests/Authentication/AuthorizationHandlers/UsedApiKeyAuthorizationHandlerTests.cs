using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SecurityDashboard_Backend.Authentication;
using SecurityDashboard_Backend.Authentication.AuthorizationHandlers;

namespace SecurityDashboard_Backend.Tests.Authentication.AuthorizationHandlers;

public class UsedApiKeyAuthorizationHandlerTests
{
    private IAuthorizationService BuildAuthorizationService(
        Action<IServiceCollection> setupServices = null)
    {
        var services = new ServiceCollection();
        services.AddAuthorization();
        services.AddLogging();
        services.AddOptions();
        setupServices?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithAuthorizedUser_ShouldSucceed()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.UsedApiKey, policy => policy.AddRequirements(new UsedApiKeyRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, UsedApiKeyAuthorizationHandler>();
        });
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, ApiKeyProvider.ApiKeyOwnerName)],
            ApiKeyDefaults.AuthenticationScheme));

        // Act
        var result = await authorizationService.AuthorizeAsync(userPrincipal, Policies.UsedApiKey);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUnauthorizedUser_ShouldFail()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.UsedApiKey, policy => policy.AddRequirements(new UsedApiKeyRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, UsedApiKeyAuthorizationHandler>();
        });
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await authorizationService.AuthorizeAsync(userPrincipal, Policies.UsedApiKey);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUserNotAuthorizedViaApiKey_ShouldFail()
    {
        // Arrange
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.UsedApiKey, policy => policy.AddRequirements(new UsedApiKeyRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, UsedApiKeyAuthorizationHandler>();
        });
        var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, ApiKeyProvider.ApiKeyOwnerName)],
            BearerTokenDefaults.AuthenticationScheme));

        // Act
        var result = await authorizationService.AuthorizeAsync(userPrincipal, Policies.UsedApiKey);

        // Assert
        result.Succeeded.Should().BeFalse();
    }
}
