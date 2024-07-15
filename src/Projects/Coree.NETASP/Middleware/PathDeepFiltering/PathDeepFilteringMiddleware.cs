using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.PathDeep
{
    public class PathDeepFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<PathDeepFilteringMiddleware> _logger;
        private readonly PathDeepFilteringOptions _options;
        private readonly IPointService _pointService;

        public PathDeepFilteringMiddleware(RequestDelegate nextMiddleware, IOptions<PathDeepFilteringOptions> options, ILogger<PathDeepFilteringMiddleware> logger, IPointService pointService)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
            _pointService = pointService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            
            var requestPath = context.Request.Path.ToString();
            var pathDepth = CalculatePathDepth(requestPath);



            var isAllowed = _options.PathDeepLimit >= pathDepth;

            if (isAllowed)
            {
                _logger.LogDebug("Pathdeep: a path deep of {pathDepth} is allowed.", pathDepth);
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(PathDeepFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(PathDeepFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(PathDeepFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogDebug("Pathdeep: a path deep of {pathDepth} is not allowed.", pathDepth);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }

        private int CalculatePathDepth(string path)
        {
            var normalizedPath = TrimSpecific(path, '/', 1, 1);
            if (String.IsNullOrEmpty(normalizedPath))
            {
                return 0;
            }
            var res = normalizedPath.Split('/', StringSplitOptions.None);
            return res.Length;
        }

        public string TrimSpecific(string input, char charToTrim, int countFromStart, int countFromEnd)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int start = 0;
            int end = input.Length;

            // Trim from start
            while (start < end && input[start] == charToTrim && countFromStart > 0)
            {
                start++;
                countFromStart--;
            }

            // Trim from end
            while (end > start && input[end - 1] == charToTrim && countFromEnd > 0)
            {
                end--;
                countFromEnd--;
            }

            if (start >= end)
                return string.Empty;

            return input.Substring(start, end - start);
        }
    }

    public static class PathDeepFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddPathDeepFiltering(this IServiceCollection services, int pathDeepLimit = 4,bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<PathDeepFilteringOptions>(options =>
            {
                options.PathDeepLimit = pathDeepLimit;
                options.ContinueOnDisallowed = continueOnDisallowed;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.DisallowedStatusCode = disallowedStatusCode;
            });

            return services;
        }
    }

    public class PathDeepFilteringOptions
    {
        public int PathDeepLimit { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }
}
