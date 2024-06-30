using Coree.NETASP.Extensions.HttpResponsex;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptLanguageFilteringMiddleware"/> class.
        /// </summary>
        /// <param name="nextMiddleware">The next middleware component in the HTTP request processing pipeline.</param>
        /// <param name="logger">The logger for logging information, warnings, and errors.</param>
        /// <param name="options">The options containing the whitelist and blacklist of languages.</param>
        public AcceptLanguageFilteringMiddleware(RequestDelegate nextMiddleware, ILogger<AcceptLanguageFilteringMiddleware> logger, IOptions<AcceptLanguageFilteringOptions> options)
        {
            _nextMiddleware = nextMiddleware;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Processes a request to determine if the 'Accept-Language' header is in the whitelist or not in the blacklist before passing it to the next middleware.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task that represents the asynchronous operation of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string languageHeader = context.Request.Headers["Accept-Language"].ToString();

            var isAllowedLanguage = languageHeader.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), _options.Blacklist?.ToList());

            if (isAllowedLanguage)
            {
                _logger.LogDebug("Accept-Language: '{AcceptLanguage}' is allowed.", languageHeader);
                await _nextMiddleware(context);
                return;
            }

            _logger.LogError("Accept-Language: '{AcceptLanguage}' is not allowed.", languageHeader);
            await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
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
        public static IServiceCollection AddAcceptLanguageFiltering(this IServiceCollection services, string[]? whitelist = null, string[]? blacklist = null)
        {
            services.Configure<AcceptLanguageFilteringOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
            });

            return services;
        }
    }
}