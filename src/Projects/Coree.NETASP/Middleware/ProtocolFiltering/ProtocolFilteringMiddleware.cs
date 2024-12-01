using System.Text.Json;

using Coree.NETASP.Extensions;
using Coree.NETASP.Services.Points;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Middleware.ProtocolFiltering
{
    /// <summary>
    /// Middleware to filter requests based on the HTTP protocol used.
    /// </summary>
    public class ProtocolFilteringMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<ProtocolFilteringMiddleware> _logger;
        private readonly IOptionsMonitor<ProtocolFilteringMiddlewareOptions> _optionsMonitor;
        private ProtocolFilteringMiddlewareOptions _options;
        private readonly IPointService _pointService;

        public ProtocolFilteringMiddleware(IOptionsMonitor<ProtocolFilteringMiddlewareOptions> optionsMonitor,RequestDelegate nextMiddleware, ILogger<ProtocolFilteringMiddleware> logger, IPointService pointService , IServiceProvider serviceProvider)
        {
            _nextMiddleware = nextMiddleware;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
            _options = _optionsMonitor.CurrentValue;
            _pointService = pointService;
            _optionsMonitor.OnChange(updatedOptions => { _options = updatedOptions; 
                _logger.LogDebug("Configuration for {OptionsName} updated.", nameof(ProtocolFilteringMiddlewareOptions));}
            );
        }

        /// <summary>
        /// Invoke method to process the HTTP context based on allowed protocols.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string protocol = context.Request.Protocol;

            var isAllowed = protocol.ValidateWhitelistBlacklist(_options.Whitelist?.ToList(), _options.Blacklist?.ToList());

            if (isAllowed)
            {
                _logger.LogDebug("Protocol: '{Protocol}' is allowed.", protocol);
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

                await _pointService.AddOrUpdateEntry(requestIp, _options.DisallowedFailureRating, $"{nameof(ProtocolFilteringMiddleware)} added {_options.DisallowedFailureRating} Point for IPs {requestIp}.");
                _logger.LogDebug("{MiddlewareName} added {DisallowedFailureRating} FailureRatingPoints for IPs {requestIp}.", nameof(ProtocolFilteringMiddleware) , _options.DisallowedFailureRating, requestIp);

                if (_options.ContinueOnDisallowed)
                {
                    _logger.LogDebug("Failed on {MiddlewareName} but continue.", nameof(ProtocolFilteringMiddleware));
                    await _nextMiddleware(context);
                    return;
                }
                else
                {
                    _logger.LogError("Protocol: '{Protocol}' is not allowed.", protocol);
                    await context.Response.WriteDefaultStatusCodeAnswer(_options.DisallowedStatusCode);
                    return;
                }
            }
        }
    }

    public class ProtocolFilteringMiddlewareOptions
    {
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
        public int DisallowedStatusCode { get; set; }
        public int DisallowedFailureRating { get; set; }
        public bool ContinueOnDisallowed { get; set; }
    }

    public static class ProtocolFilteringMiddlewareExtensions
    {
        public static IServiceCollection ConfigureProtocolFilteringMiddleware(this IServiceCollection services, string[]? whitelist = null, string[]? blacklist = null, bool continueOnDisallowed = false, int disallowedFailureRating = 10, int disallowedStatusCode = StatusCodes.Status400BadRequest)
        {
            whitelist ??= new string[] { "HTTP/1.1", "HTTP/2", "HTTP/2.0", "HTTP/3", "HTTP/3.0" };
            blacklist ??= new string[] { "", "HTTP/1.0", "HTTP/1.?" };

            services.Configure<ProtocolFilteringMiddlewareOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
                options.ContinueOnDisallowed = continueOnDisallowed;
                options.DisallowedFailureRating = disallowedFailureRating;
                options.DisallowedStatusCode = disallowedStatusCode;
            });

            return services;
        }

        public static IServiceCollection ConfigureProtocolFilteringMiddleware(this IServiceCollection services, string directoryPath)
        {
            // Ensure the directory path ends with a directory separator
            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                directoryPath += Path.DirectorySeparatorChar;

            // Construct the file name from the ProtocolFilteringOptions class name
            string fileName = nameof(ProtocolFilteringMiddlewareOptions) + ".json";
            string fullPath = directoryPath + fileName;

            // Check if the JSON file exists
            if (!File.Exists(fullPath))
            {
                // Create default configuration object
                ProtocolFilteringMiddlewareOptions defaultOptions = new ProtocolFilteringMiddlewareOptions
                {
                    Whitelist = new string[] { "HTTP/1.1", "HTTP/2", "HTTP/2.0", "HTTP/3", "HTTP/3.0" },
                    Blacklist = new[] { "", "HTTP/1.0", "HTTP/1.?" },
                    ContinueOnDisallowed = true,
                    DisallowedFailureRating = 10,
                    DisallowedStatusCode = 400
                };

                string topLevelKey = nameof(ProtocolFilteringMiddlewareOptions);
                var optionsDictionary = new Dictionary<string, object> { { topLevelKey, defaultOptions } };

                string defaultJson = JsonSerializer.Serialize(optionsDictionary, new JsonSerializerOptions { WriteIndented = true });

                // Create the file with default settings
                File.WriteAllText(fullPath, defaultJson);

            }

            // Create a configuration object
            var configuration = new ConfigurationBuilder().AddJsonFile(fullPath, optional: false, reloadOnChange: true).Build();
            
            // Bind and configure the ProtocolFilteringOptions using the loaded configuration
            services.Configure<ProtocolFilteringMiddlewareOptions>(configuration.GetSection("ProtocolFilteringMiddlewareOptions"));

            return services;
        }
    }



}