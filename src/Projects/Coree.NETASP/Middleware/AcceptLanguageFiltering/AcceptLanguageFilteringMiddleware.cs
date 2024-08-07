﻿using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.AcceptLanguageFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the 'Accept-Language' header values against a configurable whitelist and blacklist.
    /// </summary>
    public class AcceptLanguageFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<AcceptLanguageFilteringMiddleware> _logger;
        private readonly AcceptLanguageFilteringOptions _options;
        private readonly IPointService _pointService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptLanguageFilteringMiddleware"/> class.
        /// </summary>
        /// <param name="nextMiddleware">The next middleware component in the HTTP request processing pipeline.</param>
        /// <param name="logger">The logger for logging information, warnings, and errors.</param>
        /// <param name="options">The options containing the whitelist and blacklist of languages.</param>
        public AcceptLanguageFilteringMiddleware(RequestDelegate nextMiddleware, ILogger<AcceptLanguageFilteringMiddleware> logger, IOptions<AcceptLanguageFilteringOptions> options, IPointService pointService)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
            _pointService = pointService;
        }

        /// <summary>
        /// Processes a request to determine if the 'Accept-Language' header is in the whitelist or not in the blacklist before passing it to the next middleware.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task that represents the asynchronous operation of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string? languageHeader = context.Request.Headers.AcceptLanguage.FirstOrDefault()?.ToString();
            languageHeader ??= String.Empty;

            var isAllowed = languageHeader.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), _options.Blacklist?.ToList());

            if (isAllowed)
            {
                _logger.LogDebug("Accept-Language: '{AcceptLanguage}' is allowed.", languageHeader);
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(AcceptLanguageFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(AcceptLanguageFilteringMiddleware), _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(AcceptLanguageFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("Accept-Language: '{AcceptLanguage}' is not allowed.", languageHeader);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Represents the configurable options for filtering languages in <see cref="AcceptLanguageFilteringMiddleware"/>.
    /// </summary>
    public class AcceptLanguageFilteringOptions
    {
        /// <summary>
        /// Gets or sets an array of language codes that are allowed to pass through the middleware.
        /// </summary>
        /// <value>An array of strings, each representing a language code in the whitelist.</value>
        public string[]? Whitelist { get; set; }

        /// <summary>
        /// Gets or sets an array of language codes that are not allowed to pass through the middleware.
        /// </summary>
        /// <value>An array of strings, each representing a language code in the blacklist.</value>
        public string[]? Blacklist { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }

    /// <summary>
    /// Extension methods for adding the <see cref="AcceptLanguageFilteringMiddleware"/> to the application's request pipeline.
    /// </summary>
    public static class AcceptLanguageFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddAcceptLanguageFiltering(this IServiceCollection services, string[]? whitelist = null, string[]? blacklist = null, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            services.Configure<AcceptLanguageFilteringOptions>(options =>
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
}