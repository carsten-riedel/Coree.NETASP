using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.RequestUrlFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class RequestUrlFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<RequestUrlFilteringMiddleware> _logger;
        private readonly RequestUrlFilteringOptions _options;
        private readonly IPointService _pointService;

        public RequestUrlFilteringMiddleware(RequestDelegate nextMiddleware, IOptions<RequestUrlFilteringOptions> options, ILogger<RequestUrlFilteringMiddleware> logger, IPointService pointService)
        {
            _nextMiddleware = nextMiddleware;
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
            var uriPath = GetFullRequestUri(context)?.LocalPath;
            uriPath ??= String.Empty;

            var isAllowed = uriPath.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), _options.Blacklist?.ToList());

            if (uriPath == string.Empty)
            {
                isAllowed = false;
            }

            if (isAllowed)
            {
                _logger.LogDebug("RequestUrl: '{Request}' is allowed.", uriPath);
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(RequestUrlFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(RequestUrlFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(RequestUrlFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("RequestUrl: '{Request}' is not allowed.", uriPath);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }

        /// <summary>
        /// Builds the complete URI from the request components.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <returns>The full URI of the request or null if URI is invalid.</returns>
        public Uri? GetFullRequestUri(HttpContext context)
        {
            try
            {
                var request = context.Request;

                // Validation checks
                if (string.IsNullOrEmpty(request.Scheme) || string.IsNullOrEmpty(request.Host.Host))
                {
                    return null;
                }

                // Build the full URI
                var uriBuilder = new UriBuilder
                {
                    Scheme = request.Scheme,
                    Host = request.Host.Host,
                    Port = request.Host.Port ?? -1, // Keep default port handling
                    Path = request.PathBase.Add(request.Path).ToString(),
                    Query = request.QueryString.ToString()
                };

                return uriBuilder.Uri;
            }
            catch (Exception ex)
            {
                // Log the exception details
                _logger.LogDebug(ex, "Failed to build full request URI. {DisplayUrl}", context.Request.GetDisplayUrl());
                return null; // or handle as appropriate
            }
        }
    }

    public static class RequestUrlFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddRequestUrlFiltering(this IServiceCollection services, string[]? whitelist = null, string[]? blacklist = null, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<RequestUrlFilteringOptions>(options =>
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

    public class RequestUrlFilteringOptions
    {
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }
}