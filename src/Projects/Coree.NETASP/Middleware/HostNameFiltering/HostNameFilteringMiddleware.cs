using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.HostNameFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class HostNameFilteringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HostNameFilteringMiddleware> _logger;
        private readonly string[]? _allowedHosts;

        public HostNameFilteringMiddleware(RequestDelegate next, IOptions<HostNameFilterOptions> options, ILogger<HostNameFilteringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _allowedHosts = options.Value.AllowedHosts;
        }

        /// <summary>
        /// Invoke method to process the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var requestIp = context.Connection.RemoteIpAddress;

            if (request.Host.HasValue)
            {
                // Check if the request host matches any allowed host (including wildcards)
                if (_allowedHosts is not null && _allowedHosts.Any(allowedHost => IsHostAllowed(request.Host.Host, allowedHost)))
                {
                    _logger.LogDebug("Request host: '{RequestHost}' is allowed.", request.Host.Host);
                    await _next(context);
                    return;
                }
            }

            _logger.LogError("Request host: '{RequestHost}' is not allowed.", request.Host.Host);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Forbidden: Not allowed.");
        }

        /// <summary>
        /// Checks if the request host matches the allowed host pattern.
        /// </summary>
        /// <param name="requestHost">The request host.</param>
        /// <param name="allowedHost">The allowed host pattern.</param>
        /// <returns>True if the host is allowed; otherwise, false.</returns>
        private bool IsHostAllowed(string requestHost, string allowedHost)
        {
            var pattern = "^" + Regex.Escape(allowedHost).Replace(@"\*", ".*") + "$";
            return Regex.IsMatch(requestHost, pattern, RegexOptions.IgnoreCase);
        }
    }
}