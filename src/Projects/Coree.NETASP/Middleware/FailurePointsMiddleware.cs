using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware
{
    public class FailurePointsMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<FailurePointsMiddleware> _logger;
        private readonly IPointService _pointService;
        private readonly FailurePointsMiddlewareOptions _options;

        public FailurePointsMiddleware(RequestDelegate nextMiddleware, ILogger<FailurePointsMiddleware> logger,IOptions<FailurePointsMiddlewareOptions> options, IPointService pointService)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _pointService = pointService;
            _options = options.Value;
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

            var uriPath = GetFullRequestUri(context)?.LocalPath;
            uriPath ??= String.Empty;

            bool isAllowed = false;

            if (uriPath == String.Empty)
            {
                isAllowed = false;
            }
            else
            {
                isAllowed = uriPath.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), (new string[] { "*" })?.ToList());
            }

            if (isAllowed)
            {
                _logger.LogDebug("RequestUrl: '{Request}' is allowed.", uriPath);
                await _nextMiddleware(context);
                return;
            }

            Entry? entrys = await _pointService.GetEntry(requestIp);

            if (entrys == null)
            {
                await _nextMiddleware(context);
                return;
            }

            await _pointService.ShrinkEntrys(requestIp);

            var pointsIndividual = (ulong)entrys.PointEntries.Sum(e => e.Points);
            var pointsSession = entrys.PointEntriesAccumated;
            var pointsAll = entrys.PointAllTime;

            var poinsTotal = pointsIndividual + pointsSession + pointsAll;
            var pointCurrent = pointsIndividual + pointsSession;

            _logger.LogDebug("{MiddlewareName}: Request Ip: {requestIp} has Total: {poinsTotal} Current: {pointCurrent} failure points.", nameof(FailurePointsMiddleware), requestIp, poinsTotal, pointCurrent);

            if (poinsTotal == 0)
            {
                await _nextMiddleware(context);
                return;
            }
            else
            {
                _logger.LogError("{MiddlewareName}: Request Ip: {requestIp} has Total: {poinsTotal} Current: {pointCurrent} failure points.", nameof(FailurePointsMiddleware), requestIp, poinsTotal, pointCurrent);
                await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                return;
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

    public static class FailurePointsMiddlewareExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to. This collection will be enhanced by the configuration of the HostNameFilteringMiddleware.</param>
        /// <param name="whitelist">An array of strings specifying the hostnames that should be allowed by the middleware. This list directly populates the Whitelist property of the <see cref="HostNameFilterOptions"/>.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained, enabling fluent configuration.</returns>
        public static IServiceCollection AddFailurePoints(this IServiceCollection services, string[]? whitelist = null, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<FailurePointsMiddlewareOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.DisallowedStatusCode = disallowedStatusCode;
            });

            return services;
        }
    }

    public class FailurePointsMiddlewareOptions
    {
        public string[]? Whitelist { get; set; }
        public int DisallowedStatusCode { get; set; }
    }
}