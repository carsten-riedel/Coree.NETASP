using System.Text.RegularExpressions;

using Coree.NETASP.Extensions.HttpResponsex;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.RequestUrlFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class RequestUrlFilteringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestUrlFilteringMiddleware> _logger;
        private readonly RequestUrlFilteringOptions _filters;

        public RequestUrlFilteringMiddleware(RequestDelegate next, IOptions<RequestUrlFilteringOptions> options, ILogger<RequestUrlFilteringMiddleware> logger)
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
            var uri = GetFullRequestUri(context);
            var uriPath = uri.LocalPath;
            var result = uriPath.ValidateWhitelistBlacklist(_filters.Whitelist, _filters.Blacklist);

            if (result)
            {
                _logger.LogDebug("Request: '{Request}' is allowed.", uriPath);
                await _next(context);
                return;
            }

            _logger.LogError("Request: '{Request}' is not allowed.", uriPath);
            await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
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