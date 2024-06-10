using AspNetCore.Authentication.ApiKey;
using System.Security.Claims;

namespace SecurityDashboard_Backend.Authentication;

public class ApiKey : IApiKey
{
    public ApiKey(string key, string owner, List<Claim>? claims = null)
    {
        Key = key;
        OwnerName = owner;
        Claims = claims ?? [];
    }

    public string Key { get; }
    public string OwnerName { get; }
    public IReadOnlyCollection<Claim> Claims { get; }
}
