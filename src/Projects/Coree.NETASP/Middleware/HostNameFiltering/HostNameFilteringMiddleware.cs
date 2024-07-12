using System.Text.RegularExpressions;

using Coree.NETASP.Extensions.HttpResponsex;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.HostNameFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the host header.
    /// </summary>
    public class HostNameFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<HostNameFilteringMiddleware> _logger;
        private readonly HostNameFilterOptions _options;
        private readonly IPointService _pointService;

        public HostNameFilteringMiddleware(RequestDelegate nextMiddleware, ILogger<HostNameFilteringMiddleware> logger, IOptions<HostNameFilterOptions> options, IPointService pointService)
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
            var request = context.Request;
            string? requestIp = context.Connection.RemoteIpAddress?.ToString();
            if (requestIp == null)
            {
                _logger.LogError("Request Ip: Request without IPs are not allowed.");
                await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status422UnprocessableEntity);
                return;
            }

            if (request.Host.HasValue)
            {
                var isAllowed = request.Host.Host.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), new List<string>() { "*" });
                // Check if the request host matches any allowed host (including wildcards)
                if (isAllowed)
                {
                    _logger.LogDebug("Request host: '{RequestHost}' is allowed.", request.Host.Host);
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _pointService.AssignFailureRating(requestIp, _options.DisallowedFailureRating, $"HostNameFiltering added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                    _logger.LogDebug("HostNameFiltering added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", _options.DisallowedFailureRating, requestIp);
                    if (_options.ContinueOnDisallowed)
                    {
                        _logger.LogDebug("Failed on HostNameFiltering but continue.");
                        await _nextMiddleware(context);
                        return;
                    }
                    else
                    {
                        _logger.LogError("Request host: '{RequestHost}' is not allowed.", request.Host.Host);
                        await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                        return;
                    }
                }
            }

            _logger.LogError("Request host: '{RequestHost}' is not allowed.", request.Host.Host);
            await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
        }
    }

    /// <summary>
    /// Contains extension methods for registering the <see cref="HostNameFilteringMiddleware"/> in an application's service collection.
    /// These methods provide a convenient way to configure and apply hostname-based request filtering directly from the application startup.
    /// </summary>
    public static class HostNameFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to. This collection will be enhanced by the configuration of the HostNameFilteringMiddleware.</param>
        /// <param name="whitelist">An array of strings specifying the hostnames that should be allowed by the middleware. This list directly populates the Whitelist property of the <see cref="HostNameFilterOptions"/>.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained, enabling fluent configuration.</returns>
        public static IServiceCollection AddHostNameFiltering(this IServiceCollection services, string[]? whitelist = null, string[]? blacklist = null,bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<HostNameFilterOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
                options.DisallowedStatusCode = disallowedStatusCode;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.ContinueOnDisallowed = continueOnDisallowed;
            });

            return services;
        }
    }

    /// <summary>
    /// Provides configuration settings for filtering requests based on hostname values.
    /// This class represents the options used by <see cref="HostNameFilteringMiddleware"/> to determine
    /// which hostnames are allowed to access the application.
    /// </summary>
    public class HostNameFilterOptions
    {
        /// <summary>
        /// Gets or sets an array of hostnames that are allowed to access the application.
        /// </summary>
        /// <value>An array of strings, each representing a hostname in the whitelist.</value>
        public string[]? Whitelist { get; set; }

        public string[]? Blacklist { get; set; }

        public int DisallowedStatusCode { get; set; }

        public int DisallowedFailureRating { get; set; }

        public bool ContinueOnDisallowed { get; set; }
    }



}