using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.UserAgentFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class UserAgentFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<UserAgentFilteringMiddleware> _logger;
        private readonly UserAgentFilterOptions _options;
        private readonly IPointService _pointService;

        public UserAgentFilteringMiddleware(RequestDelegate nextMiddleware, IOptions<UserAgentFilterOptions> options, ILogger<UserAgentFilteringMiddleware> logger, IPointService pointService)
        {
            this._nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
            _pointService = pointService;
        }
   

        /// <summary>
        /// Invoke method to process the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string? userAgentHeader = context.Request.Headers.UserAgent.FirstOrDefault()?.ToString();
            userAgentHeader ??= String.Empty;
            
            var isAllowed = userAgentHeader.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), _options.Blacklist?.ToList());

            if (isAllowed)
            {
                _logger.LogDebug("Useragent: '{userAgentHeader}' is allowed.", userAgentHeader);
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(UserAgentFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(UserAgentFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(UserAgentFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("Useragent: '{userAgentHeader}' is not allowed.", userAgentHeader);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }
    }

    public static class UserAgentFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddUserAgentFiltering(this IServiceCollection services, string[]? whitelist = null, string[]? blacklist = null, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<UserAgentFilterOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
                options.ContinueOnDisallowed = continueOnDisallowed;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.DisallowedStatusCode = disallowedStatusCode;

            });

            return services;
        }
    }

    public class UserAgentFilterOptions
    {
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }
}