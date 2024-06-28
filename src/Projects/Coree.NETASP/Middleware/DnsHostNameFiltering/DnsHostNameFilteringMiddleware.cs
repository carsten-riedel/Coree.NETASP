using System.Net;

using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.DnsHostNameFiltering
{
    public class DnsHostNameFilteringOptions
    {
        public string? FailedResolutionString { get; set; }
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
    }

    /// <summary>
    /// Middleware to filter requests based on the HTTP protocol used.
    /// </summary>
    public class DnsHostNameFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<DnsHostNameFilteringMiddleware> _logger;
        private readonly DnsHostNameFilteringOptions _options;

        public DnsHostNameFilteringMiddleware(RequestDelegate nextMiddleware, ILogger<DnsHostNameFilteringMiddleware> logger, IOptions<DnsHostNameFilteringOptions> options)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Invoke method to process the HTTP context based on allowed protocols.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string? remoteIPAddress = context.Connection.RemoteIpAddress?.ToString();
            string? resolvedDnsName;
            if (remoteIPAddress != null)
            {
                try
                {
                    var iPHostEntry = Dns.GetHostEntry(remoteIPAddress);
                    resolvedDnsName = iPHostEntry.HostName;
                }
                catch (Exception)
                {
                    resolvedDnsName = _options.FailedResolutionString;
                }
            }
            else
            {
                resolvedDnsName = _options.FailedResolutionString;
            }

            bool isAllowed = false;

            if (resolvedDnsName != null && resolvedDnsName != _options.FailedResolutionString)
            {
                isAllowed = resolvedDnsName.ValidateWhitelistBlacklist(_options.Whitelist?.ToList()?.ToList(), _options.Blacklist?.ToList());
            }

            if (isAllowed)
            {
                _logger.LogDebug("DNS Host Name: '{ResolvedDnsName}' is allowed.", resolvedDnsName);
                await _nextMiddleware(context);
                return;
            }

            _logger.LogError("DNS Host Name: '{ResolvedDnsName}' is not allowed.", resolvedDnsName);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Forbidden: Not allowed.");
        }
    }

    public static class DnsHostNameFilteringMiddlewareExtensions
    {
        public static IServiceCollection AddDnsHostNameFiltering(this IServiceCollection services, string? failedResolution = "Unresolvable", string[]? whitelist = null, string[]? blacklist = null)
        {
            if (failedResolution != null)
            {
                blacklist ??= new string[] { failedResolution };
            }

            services.Configure<DnsHostNameFilteringOptions>(options =>
            {
                options.FailedResolutionString = failedResolution;
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
            });

            return services;
        }
    }
}