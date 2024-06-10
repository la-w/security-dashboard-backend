using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SecurityDashboard_Backend.Authentication.AuthorizationHandlers;

public class UsedApiKeyRequirement : IAuthorizationRequirement;

public class UsedApiKeyAuthorizationHandler : AuthorizationHandler<UsedApiKeyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UsedApiKeyRequirement requirement)
    {
        if (context.User.Identity?.AuthenticationType == ApiKeyDefaults.AuthenticationScheme
            && context.User.HasClaim(ClaimTypes.Name, ApiKeyProvider.ApiKeyOwnerName))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
