using System.Security.Cryptography;
using System.Text;

using Coree.NETASP.Extensions;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.UnderConstruction
{
    /// <summary>
    /// Middleware to validate the TFTS (Time Frame Token Service) header token.
    /// </summary>
    public class TimeFrameTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TimeFrameTokenMiddleware> _logger;
        private readonly TimeFrameTokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeFrameTokenMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger for this middleware.</param>
        /// <param name="options">The options for the TimeFrameTokenMiddleware.</param>
        /// <exception cref="ArgumentNullException">Thrown when options are null.</exception>
        public TimeFrameTokenMiddleware(RequestDelegate next, ILogger<TimeFrameTokenMiddleware> logger, IOptions<TimeFrameTokenOptions> options)
        {
            _next = next;
            _logger = logger;
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _tokenService = new TimeFrameTokenService(options.Value);
        }

        /// <summary>
        /// Invokes the middleware to validate the TFTS header token.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Retrieve the token from the "TFTS" header and convert it to a string.
            string tokenValue = context.Request.Headers[_tokenService.HeaderName].ToString();

            // Check if the token is missing or empty.
            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                _logger.LogError("TFTS header token is missing or empty.");
                await context.Response.WriteDefaultStatusCodeAnswer(404);
                return;
            }

            // Validate the token using the token service.
            bool isValid = _tokenService.ValidateToken(tokenValue);

            if (isValid)
            {
                _logger.LogDebug("TFTS validation succeeded; continuing processing.");
                await _next(context);
            }
            else
            {
                _logger.LogError("TFTS validation failed; returning 404.");
                await context.Response.WriteDefaultStatusCodeAnswer(404);
            }
        }
    }

    /// <summary>
    /// Extension methods for configuring the TimeFrameTokenMiddleware.
    /// </summary>
    public static class TimeFrameTokenServiceExtensions
    {
        /// <summary>
        /// Configures the TimeFrameTokenMiddleware with the specified options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">A delegate to configure the TimeFrameTokenOptions.</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown if configureOptions is null.</exception>
        public static IServiceCollection ConfigureTimeFrameTokenServices(
            this IServiceCollection services,
            Action<TimeFrameTokenOptions> configureOptions)
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions), "Configuration action cannot be null.");
            }

            services.Configure(configureOptions);
            return services;
        }


        public static IApplicationBuilder UseTimeFrameTokenMiddleware(this IApplicationBuilder app)
        {
            // Retrieve existing configuration
            var options = app.ApplicationServices.GetService<IOptions<TimeFrameTokenOptions>>();

            // If no configuration exists, throw an error
            if (options == null || options.Value == null)
            {
                throw new InvalidOperationException(
                    "TimeFrameTokenOptions are not configured. Ensure they are registered in ConfigureServices before using the middleware."
                );
            }

            return app.UseMiddleware<TimeFrameTokenMiddleware>();
        }




        public static IServiceCollection AddHttpTimeframeTokenClient(this IServiceCollection services, Uri baseAddress, Action<TimeFrameTokenOptions>? configureOptions = null)
        {
            // Check if TimeFrameTokenOptions are already configured
            var existingConfig = services.BuildServiceProvider().GetService<IOptions<TimeFrameTokenOptions>>();

            // If no config exists AND no options are provided, throw an error
            if (existingConfig == null && configureOptions == null)
            {
                throw new InvalidOperationException("TimeFrameTokenOptions are not configured. Please configure them before adding the HttpClient.");
            }

            // If config exists AND configureOptions is provided, throw an error to prevent conflicts
            if (existingConfig != null && configureOptions != null)
            {
                throw new InvalidOperationException("TimeFrameTokenOptions are already configured. Remove existing configuration before overriding.");
            }

            // If no config exists but options are provided, apply them
            if (existingConfig == null && configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Add the TimeFrameTokenHandler to the service collection
            services.AddTransient<TimeFrameTokenHandler>();

            // Add the HttpClient with the TimeFrameTokenHandler
            services.AddHttpClient("HttpTimeframeTokenClient", client =>
            {
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<TimeFrameTokenHandler>();

            // Ensure the TimeFrameTokenService is added to the service collection
            services.AddSingleton(provider =>
            {
                var optionsMonitor = provider.GetService<IOptions<TimeFrameTokenOptions>>();
                if (optionsMonitor == null || optionsMonitor.Value == null)
                {
                    throw new InvalidOperationException("TimeFrameTokenOptions are not configured. Please use the ConfigureTimeFrameTokenServices extension method to configure the options.");
                }

                return new TimeFrameTokenService(optionsMonitor.Value);
            });

            return services;
        }




   
    }

    /// <summary>
    /// Options for the TimeFrameTokenMiddleware.
    /// </summary>
    public class TimeFrameTokenOptions
    {
        /// <summary>
        /// Gets or sets the secret for the TimeFrameTokenMiddleware.
        /// </summary>
        public string Secret { get; set; } = "secret";

        /// <summary>
        /// Gets or sets the interval at which the token key changes.
        /// </summary>
        public TimeSpan KeyChangeInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the time window during which tokens are considered valid.
        /// </summary>
        public TimeSpan KeyComparisonInterval { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the name of the header.
        /// </summary>
        public string HeaderName { get; set; } = "TFTS";
    }


    public class TimeFrameTokenHandler : DelegatingHandler
    {
        private readonly TimeFrameTokenService _tokenService;

        public TimeFrameTokenHandler(TimeFrameTokenService tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Generate the current token
            string token = _tokenService.GenerateCurrentToken();

            // Add the token to the request headers
            request.Headers.Add(_tokenService.HeaderName, token);

            return await base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Provides token generation and validation based on time intervals and a shared secret.
    /// </summary>
    /// <remarks>
    /// This service uses a fixed salt and generates tokens using HMAC-SHA256. The parameters
    /// (shared secret and time intervals) can be updated at runtime using the UpdateParameters method.
    /// </remarks>
    public class TimeFrameTokenService
    {
        private const string FixedSalt = "09BB2E93-0F13-4204-9726-CCDEE9C8DC4B";
        private string _sharedSecret;
        private TimeSpan _keyChangeInterval;
        private TimeSpan _keyComparisonInterval;
        private string _headerName;

        /// <summary>
        /// Gets the name of the header.
        /// </summary>
        public string HeaderName => _headerName;

        /// <summary>
        /// Initializes a new instance of the TimeFrameTokenService class.
        /// </summary>
        /// <param name="options">The options for the TimeFrameTokenService.</param>
        public TimeFrameTokenService(TimeFrameTokenOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (string.IsNullOrEmpty(options.Secret))
            {
                throw new ArgumentException("Shared secret must not be null or empty", nameof(options.Secret));
            }
            _sharedSecret = options.Secret;
            _keyChangeInterval = options.KeyChangeInterval;
            _keyComparisonInterval = options.KeyComparisonInterval;
            _headerName = options.HeaderName;
        }

        /// <summary>
        /// Updates the parameters (shared secret, time intervals, and header name) for token generation.
        /// </summary>
        /// <param name="sharedSecret">The new shared secret to use.</param>
        /// <param name="keyChangeInterval">
        /// The new key change interval. If null, defaults to 10 seconds.
        /// </param>
        /// <param name="keyComparisonInterval">
        /// The new key comparison interval. If null, defaults to 60 seconds.
        /// </param>
        /// <param name="headerName">The new header name to use. If null, defaults to "TFTS".</param>
        /// <example>
        /// <code>
        /// // Update the token service parameters
        /// tokenService.UpdateParameters("newSecret", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(120), "NewHeader");
        /// </code>
        /// </example>
        public void UpdateParameters(string sharedSecret, TimeSpan? keyChangeInterval = null, TimeSpan? keyComparisonInterval = null, string headerName = "TFTS")
        {
            if (string.IsNullOrEmpty(sharedSecret))
            {
                throw new ArgumentException("Shared secret must not be null or empty", nameof(sharedSecret));
            }
            _sharedSecret = sharedSecret;
            _keyChangeInterval = keyChangeInterval ?? TimeSpan.FromSeconds(10);
            _keyComparisonInterval = keyComparisonInterval ?? TimeSpan.FromSeconds(60);
            _headerName = headerName;
        }

        /// <summary>
        /// Generates and returns the current token for the active time interval.
        /// </summary>
        /// <returns>The current token as a hexadecimal string.</returns>
        public string GenerateCurrentToken()
        {
            // Use the current UTC time.
            string timeFactor = ComposeTimeFactor(DateTime.UtcNow);
            return ComputeHmacSignature(timeFactor);
        }

        /// <summary>
        /// Validates whether the provided token matches any token in the valid time window.
        /// </summary>
        /// <param name="token">The token to validate.</param>
        /// <returns>True if the token is valid; otherwise, false.</returns>
        public bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // Calculate the number of intervals in the key comparison window.
            int count = (int)Math.Ceiling((double)_keyComparisonInterval.Ticks / _keyChangeInterval.Ticks);
            foreach (var validToken in GenerateTokenWindow(count))
            {
                if (string.Equals(validToken, token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generates a series of tokens covering the valid time window.
        /// </summary>
        /// <param name="count">
        /// The number of tokens to generate, corresponding to the number of key-change intervals in the comparison window.
        /// </param>
        /// <returns>An enumerable of valid tokens.</returns>
        private IEnumerable<string> GenerateTokenWindow(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

            var tokens = new List<string>();
            DateTime utcNow = DateTime.UtcNow;
            // Truncate the current time using the key-change interval.
            DateTime currentTruncatedTime = new DateTime(utcNow.Ticks - utcNow.Ticks % _keyChangeInterval.Ticks, DateTimeKind.Utc);

            for (int i = 0; i < count; i++)
            {
                DateTime intervalTime = currentTruncatedTime.AddTicks(-_keyChangeInterval.Ticks * i);
                // Use the ComposeTimeFactor helper to build the token input.
                string timeFactor = ComposeTimeFactor(intervalTime);
                tokens.Add(ComputeHmacSignature(timeFactor));
            }

            return tokens;
        }

        /// <summary>
        /// Composes the time-based input by truncating the provided time to the key-change interval and appending a fixed salt.
        /// </summary>
        /// <param name="time">The time to use when generating the input factor.</param>
        /// <returns>A string representing the time factor.</returns>
        private string ComposeTimeFactor(DateTime time)
        {
            DateTime truncatedTime = new DateTime(time.Ticks - time.Ticks % _keyChangeInterval.Ticks, DateTimeKind.Utc);
            return truncatedTime.Ticks.ToString() + FixedSalt;
        }

        /// <summary>
        /// Computes the HMAC-SHA256 signature for the given message using the shared secret.
        /// </summary>
        /// <param name="message">The message to sign.</param>
        /// <returns>A hexadecimal string representation of the HMAC signature.</returns>
        private string ComputeHmacSignature(string message)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_sharedSecret)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
