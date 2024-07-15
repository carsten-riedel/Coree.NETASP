using System.Net;

using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.DnsHostNameFiltering
{


    /// <summary>
    /// Middleware to filter requests based on the HTTP protocol used.
    /// </summary>
    public class DnsHostNameFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<DnsHostNameFilteringMiddleware> _logger;
        private readonly DnsHostNameFilteringOptions _options;
        private readonly IPointService _pointService;
        

        public DnsHostNameFilteringMiddleware(RequestDelegate nextMiddleware, ILogger<DnsHostNameFilteringMiddleware> logger, IOptions<DnsHostNameFilteringOptions> options, IPointService pointService)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
            _pointService = pointService;
        }

        /// <summary>
        /// Invoke method to process the HTTP context based on allowed protocols.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string? remoteIPAddress = context.Connection.RemoteIpAddress?.ToString();
            if (remoteIPAddress == null)
            {
                _logger.LogError("Request Ip: Request without IPs are not allowed.");
                await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
                return;
            }

            string? resolvedDnsName = null;

            try
            {
                var iPHostEntry = Dns.GetHostEntry(remoteIPAddress);
                resolvedDnsName = iPHostEntry.HostName;
            }
            catch (Exception)
            {
                resolvedDnsName = _options.FailedResolutionString;
            }

            bool isAllowed = false;

            if (resolvedDnsName != null)
            {
                isAllowed = resolvedDnsName.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), _options.Blacklist?.ToList());
            }

            if (isAllowed)
            {
                _logger.LogDebug("DNS Host Name: '{ResolvedDnsName}' is allowed.", resolvedDnsName);
                await _nextMiddleware(context);
                return;
            }
            else
            {
                string? requestIp = context.Connection.RemoteIpAddress?.ToString();
                if (requestIp == null)
                {
                    _logger.LogError("Request Ip: Request without IPs are not allowed.");
                    await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
                    return;
                }

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(DnsHostNameFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(DnsHostNameFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(DnsHostNameFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("DNS Host Name: '{ResolvedDnsName}' is not allowed.", resolvedDnsName);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }
    }

    public static class DnsHostNameFilteringMiddlewareExtensions
    {
        public static IServiceCollection AddDnsHostNameFiltering(this IServiceCollection services, string? failedResolution = "Unresolvable", string[]? whitelist = null, string[]? blacklist = null, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            if (failedResolution != null && blacklist == null)
            {
                blacklist ??= new string[] { failedResolution };
            }

            if (blacklist != null && failedResolution != null)
            {
                var exBlacklist = blacklist.ToList();
                exBlacklist.Add(failedResolution);
                blacklist = exBlacklist.ToArray();
            }

            services.Configure<DnsHostNameFilteringOptions>(options =>
            {
                options.FailedResolutionString = failedResolution;
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
                options.ContinueOnDisallowed = continueOnDisallowed;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.DisallowedStatusCode = disallowedStatusCode;
            });

            return services;
        }
    }

    public class DnsHostNameFilteringOptions
    {
        public string? FailedResolutionString { get; set; }
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }
}