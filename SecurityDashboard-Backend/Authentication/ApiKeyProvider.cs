using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;

namespace SecurityDashboard_Backend.Authentication;

public class ApiKeyProvider : IApiKeyProvider
{
    public const string ApiKeyOwnerName = "SecurityDashboardApiKey";

    private readonly ILogger<IApiKeyProvider> _logger;
    private readonly ApiKeyConfiguration _apiKeyConfiguration;

    public ApiKeyProvider(ILogger<IApiKeyProvider> logger, IOptions<ApiKeyConfiguration> apiKeyConfiguration)
    {
        _logger = logger;
        _apiKeyConfiguration = apiKeyConfiguration.Value;
    }

    public Task<IApiKey?> ProvideAsync(string key)
    {
        return Task.FromResult<IApiKey?>(key == _apiKeyConfiguration.ApiKey
            ? new ApiKey(key, ApiKeyOwnerName)
            : null);
    }
}
