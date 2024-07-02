using Coree.NETASP.Extensions.HttpResponsex;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;

using System.Net;
using System.Text.RegularExpressions;

namespace Coree.NETASP.Middleware.UnwantedHeaderKeysFiltering
{
    public class UnwantedHeaderKeysOptions
    {
        public string[]? Blacklist { get; set; }
    }

    /// <summary>
    /// Middleware to filter requests based on the HTTP protocol used.
    /// </summary>
    public class UnwantedHeaderKeysMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UnwantedHeaderKeysMiddleware> _logger;
        private readonly UnwantedHeaderKeysOptions _UnwantedHeaderKeysOptions;

        public UnwantedHeaderKeysMiddleware(RequestDelegate next, ILogger<UnwantedHeaderKeysMiddleware> logger, IOptions<UnwantedHeaderKeysOptions> options)
        {
            _next = next;
            _logger = logger;
            _UnwantedHeaderKeysOptions = options.Value;
        }

        /// <summary>
        /// Invoke method to process the HTTP context based on blacklisted headers.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            foreach (var header in context.Request.Headers)
            {
                var isValid = header.Key.ValidateWhitelistBlacklist(null, _UnwantedHeaderKeysOptions.Blacklist?.ToList());
                if (!isValid)
                {
                    _logger.LogError("Access denied due to unwanted header key: {Key}", header.Key);
                    await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
                    return;
                }
            }

            _logger.LogDebug("No unwanted headers detected. Proceeding with request.");
            await _next(context);
        }
    }

    public static class UnwantedHeaderKeysMiddlewareExtensions
    {
        public static IServiceCollection AddUnwantedHeaderKeysFiltering(this IServiceCollection services, string[]? blacklist = null)
        {
            services.Configure<UnwantedHeaderKeysOptions>(options =>
            {
                options.Blacklist = blacklist;
            });

            return services;
        }
    }
}