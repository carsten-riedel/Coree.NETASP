using System.Text.RegularExpressions;

using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.SegmentBlacklistFiltering
{
    public class SegmentBlacklistFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<SegmentBlacklistFilteringMiddleware> _logger;
        private readonly SegmentBlacklistFilterOptions _options;
        private readonly IPointService _pointService;

        public SegmentBlacklistFilteringMiddleware(RequestDelegate nextMiddleware, IOptions<SegmentBlacklistFilterOptions> options, ILogger<SegmentBlacklistFilteringMiddleware> logger, IPointService pointService)
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
            var fullUri = GetFullRequestUri(context);
            if (fullUri is null)
            {
                string? requestIp = context.Connection.RemoteIpAddress?.ToString();
                if (requestIp == null)
                {
                    _logger.LogError("Request Ip: Request without IPs are not allowed.");
                    await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
                    return;
                }

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(SegmentBlacklistFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(SegmentBlacklistFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(SegmentBlacklistFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("{MiddlewareName} can not convert {display} to uri not allowed.", nameof(SegmentBlacklistFilteringMiddleware), context.Request.GetDisplayUrl());
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }

            // Check if any segment of the request URI is in the denied list
            bool isAllowed = true;
            if (_options.DeniedSegment != null)
            {
                foreach (var segment in fullUri.Segments)
                {
                    string trimmedSegment = Uri.UnescapeDataString(segment.Trim('/'));

                    foreach (var deniedSegment in _options.DeniedSegment)
                    {
                        if (!IsSegmentAllowed(trimmedSegment, deniedSegment))
                        {
                            isAllowed = false;
                            break;
                        }
                    }

                    if (!isAllowed)
                    {
                        break;
                    }
                }
            }

            if (isAllowed)
            {
                _logger.LogDebug("Segments filter allowed.");
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(SegmentBlacklistFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(SegmentBlacklistFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(SegmentBlacklistFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("{MiddlewareName} {display} an invalid segment.", nameof(SegmentBlacklistFilteringMiddleware), context.Request.GetDisplayUrl());
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
                    throw new InvalidOperationException("Request scheme or host is not valid.");
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
                _logger.LogError(ex, "Failed to build full request URI. {DisplayUrl}", context.Request.GetDisplayUrl());
                return null; // or handle as appropriate
            }
        }

        /// <summary>
        /// Checks if a URL segment matches a denied segment pattern.
        /// </summary>
        /// <param name="input">The URL segment.</param>
        /// <param name="pattern">The denied segment pattern.</param>
        /// <returns>False if the segment is denied; otherwise, true.</returns>
        private bool IsSegmentAllowed(string input, string pattern)
        {
            // Automatically allow empty segments to pass through
            if (string.IsNullOrEmpty(input))
                return true;

            // Convert the wildcard pattern into a regular expression
            var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
            return !Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }
    }

    public class SegmentBlacklistFilterOptions
    {
        public string[]? DeniedSegment { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }

    public static class SegmentBlacklistFilteringExtensions
    {
        public static IServiceCollection AddSegmentBlacklistFiltering(this IServiceCollection services, string[]? deniedSegment, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<SegmentBlacklistFilterOptions>(options =>
            {
                options.DeniedSegment = deniedSegment;
                options.ContinueOnDisallowed = continueOnDisallowed;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.DisallowedStatusCode = disallowedStatusCode;
            });

            return services;
        }
    }
}