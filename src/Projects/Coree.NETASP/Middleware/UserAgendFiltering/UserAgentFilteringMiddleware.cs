using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.UserAgentFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class UserAgentFilteringMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly ILogger<UserAgentFilteringMiddleware> _logger;
        private readonly UserAgentFilterOptions _filters;

        public UserAgentFilteringMiddleware(RequestDelegate next, IOptions<UserAgentFilterOptions> options, ILogger<UserAgentFilteringMiddleware> logger)
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
            var request = context.Request;
            var requestIp = context.Connection.RemoteIpAddress;
            string userAgentHeader = context.Request.Headers["User-Agent"].ToString();

            bool isblacklist = false;
            for (int i = 0; i < _filters.Blacklist.Length; i++)
            {
                if (IsMatch(userAgentHeader, _filters.Blacklist[i]))
                {
                   isblacklist = true;
                }
            }

            if (!isblacklist)
            {
                _logger.LogDebug("Useragent: '{userAgentHeader}' is allowed.", userAgentHeader);
                await _next(context);
                return;
            }


            _logger.LogError("Useragent: '{userAgentHeader}' is not allowed.", userAgentHeader);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Forbidden: Not allowed.");

        }

        /// <summary>
        /// Checks if the request host matches the allowed host pattern.
        /// </summary>
        /// <param name="requestHost">The request host.</param>
        /// <param name="allowedHost">The allowed host pattern.</param>
        /// <returns>True if the host is allowed; otherwise, false.</returns>
        private bool IsMatch(string requestHost, string allowedHost)
        {
            var pattern = "^" + Regex.Escape(allowedHost).Replace(@"\*", ".*") + "$";
            return Regex.IsMatch(requestHost, pattern, RegexOptions.IgnoreCase);
        }
    }


}
