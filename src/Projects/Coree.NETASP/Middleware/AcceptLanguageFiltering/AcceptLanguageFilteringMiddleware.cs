using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.AcceptLanguageFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class AcceptLanguageFilteringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AcceptLanguageFilteringMiddleware> _logger;
        private readonly AcceptLanguageFilteringOptions _filters;

        public AcceptLanguageFilteringMiddleware(RequestDelegate next, IOptions<AcceptLanguageFilteringOptions> options, ILogger<AcceptLanguageFilteringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _filters = options.Value;
        }

        /// <summary>
        /// Invoke method to process the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            
            string AcceptLanguageHeader = context.Request.Headers["Accept-Language"].ToString();

            var result = AcceptLanguageHeader.ValidateWhitelistBlacklist(_filters.Whitelist, _filters.Blacklist);

            if (result)
            {
                _logger.LogDebug("AcceptLanguage: '{AcceptLanguage}' is allowed.", AcceptLanguageHeader);
                await _next(context);
                return;
            }

            _logger.LogError("AcceptLanguage: '{AcceptLanguage}' is not allowed.", AcceptLanguageHeader);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Forbidden: Not allowed.");
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
    }
}