using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;

namespace SecurityDashboard_Backend.Authentication;

public class ApiKeyProvider : IApiKeyProvider
{
    public const string ApiKeyOwnerName = "SecurityDashboardApiKey";

    private readonly ApiKeyConfiguration _apiKeyConfiguration;

    public ApiKeyProvider(IOptions<ApiKeyConfiguration> apiKeyConfiguration)
    {
        _apiKeyConfiguration = apiKeyConfiguration.Value;
    }

    public Task<IApiKey?> ProvideAsync(string key)
    {
        return Task.FromResult<IApiKey?>(key == _apiKeyConfiguration.ApiKey
            ? new ApiKey(key, ApiKeyOwnerName)
            : null);
    }
}
