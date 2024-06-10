using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SecurityDashboard_Backend.Utilities;

// Adapted from https://learn.microsoft.com/en-us/aspnet/core/security/ip-safelist?view=aspnetcore-8.0#action-filter
public class RemoteIpCheckActionFilter : ActionFilterAttribute
{
    private readonly ILogger<RemoteIpCheckActionFilter> _logger;
    private readonly string[]? _whitelist;

    public RemoteIpCheckActionFilter(string[]? whitelist, ILogger<RemoteIpCheckActionFilter> logger)
    {
        _whitelist = whitelist;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (_whitelist == null || _whitelist.Length == 0)
        {
            _logger.LogWarning("Remote-IP Filter disabled, no IPs configured.");
            base.OnActionExecuting(context);
            return;
        }

        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;

        if (remoteIp == null)
        {
            _logger.LogWarning("No Remote IP-Address found.");
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        _logger.LogDebug("Remote IP-Address: {RemoteIp}", remoteIp);

        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        var badIp = !_whitelist.Select(IPAddress.Parse).Contains(remoteIp);

        if (badIp)
        {
            _logger.LogWarning("Forbidden Request from IP: {RemoteIp}", remoteIp);
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        base.OnActionExecuting(context);
    }
}

public static class RemoteIpCheckActionFilterExtensions
{
    [ExcludeFromCodeCoverage(Justification = "This is just to keep Program.cs a bit cleaner")]
    public static IServiceCollection AddRemoteIpCheckActionFilter(this IServiceCollection services, string[]? whitelist)
    {
        services.AddScoped(provider => new RemoteIpCheckActionFilter(whitelist, provider.GetRequiredService<ILogger<RemoteIpCheckActionFilter>>()));
        return services;
    }
}
