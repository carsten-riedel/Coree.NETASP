using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.UnwantedHeaderKeysFiltering
{


    /// <summary>
    /// Middleware to filter requests based on the HTTP protocol used.
    /// </summary>
    public class UnwantedHeaderKeysMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<UnwantedHeaderKeysMiddleware> _logger;
        private readonly UnwantedHeaderKeysOptions _options;
        private readonly IPointService _pointService;

        public UnwantedHeaderKeysMiddleware(RequestDelegate nextMiddleware, ILogger<UnwantedHeaderKeysMiddleware> logger, IOptions<UnwantedHeaderKeysOptions> options, IPointService pointService)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
            _pointService = pointService;
        }

        /// <summary>
        /// Invoke method to process the HTTP context based on blacklisted headers.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var headerKeys = context.Request.Headers.Keys.ToList();
            string failedKey = String.Empty;
            bool isAllowedKeys = true;
            foreach (var headerKey in headerKeys)
            {
                var isAllowed = headerKey.ValidateWhitelistBlacklist(null, _options.Blacklist?.ToList());
                if (!isAllowed)
                {
                    failedKey = headerKey;
                    isAllowedKeys = false;
                    break;
                }
            }

            if (isAllowedKeys)
            {
                _logger.LogDebug("No unwanted headers detected. Proceeding with request.");
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(UnwantedHeaderKeysMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(UnwantedHeaderKeysMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(UnwantedHeaderKeysMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("Access denied due to unwanted header key: {failedKey}", failedKey);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }
    }

    public static class UnwantedHeaderKeysMiddlewareExtensions
    {
        public static IServiceCollection AddUnwantedHeaderKeysFiltering(this IServiceCollection services, string[]? blacklist = null, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<UnwantedHeaderKeysOptions>(options =>
            {
                options.Blacklist = blacklist;
                options.ContinueOnDisallowed = continueOnDisallowed;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.DisallowedStatusCode = disallowedStatusCode;
            });

            return services;
        }
    }

    public class UnwantedHeaderKeysOptions
    {
        public string[]? Blacklist { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }
}