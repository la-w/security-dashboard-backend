using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using SecurityDashboard_Backend.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SecurityDashboard_Backend.Utilities;

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Policy names map to scopes
        var requiredPolicies = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Select(attr => attr.Policy)
            .Distinct()
            .ToList();

        if (!requiredPolicies.Any() || requiredPolicies is [null]) return;

        if (requiredPolicies.Any(p => p == Policies.UsedApiKey))
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized (*Check the provided ApiKey*)" });
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden (*No Permission / IP not whitelisted*)" });

            var apiKeyScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "apiKey" }
            };

            operation.Security = [new() { [apiKeyScheme] = requiredPolicies.ToList() }];
        }
        else if (requiredPolicies.Any(p => p == Policies.OAuthUser))
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

            var oauthScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oAuth" }
            };

            operation.Security = [new() { [oauthScheme] = requiredPolicies.ToList() }];
        }
    }
}
