using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware
{

    public class SegmentBlacklistFilterOptions
    {
        public string[] DeniedSegments { get; set; }
    }

    public static class SegmentBlacklistFilteringExtensions
    {

        public static IServiceCollection AddSegmentBlacklistFiltering(this IServiceCollection services, string[] deniedSegments)
        {
            services.Configure<SegmentBlacklistFilterOptions>(options =>
            {
                options.DeniedSegments = deniedSegments;
            });

            return services;
        }
    }

    public class SegmentBlacklistFilteringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SegmentBlacklistFilteringMiddleware> _logger;
        private readonly string[]? _deniedSegments;

        public SegmentBlacklistFilteringMiddleware(RequestDelegate next, IOptions<SegmentBlacklistFilterOptions> options, ILogger<SegmentBlacklistFilteringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _deniedSegments = options.Value.DeniedSegments;
        }

        /// <summary>
        /// Invoke method to process the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var fullUri = GetFullRequestUri(context);

            // Check if any segment of the request URI is in the denied list
            if (_deniedSegments != null)
            {
                foreach (var segment in fullUri.Segments)
                {
                    string trimmedSegment = Uri.UnescapeDataString(segment.Trim('/'));
                    bool isAllowed = true;

                    foreach (var deniedSegment in _deniedSegments)
                    {
                        if (!IsSegmentAllowed(trimmedSegment, deniedSegment))
                        {
                            isAllowed = false;
                            break;
                        }
                    }

                    if (!isAllowed)
                    {
                        _logger.LogError("Access denied to the URL path: '{RequestPath}'.", fullUri.PathAndQuery);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Forbidden: Not allowed.");
                        return;
                    }
                }
            }
            _logger.LogInformation("Segments filter allowed.");
            await _next(context);
        }

        /// <summary>
        /// Builds the complete URI from the request components.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <returns>The full URI of the request.</returns>
        public static Uri GetFullRequestUri(HttpContext context)
        {
            var request = context.Request;

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
}
