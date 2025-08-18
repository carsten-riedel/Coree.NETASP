//using Coree.NETASP.Extensions;
//using Coree.NETASP.Services.Points;
//using Coree.NETASP.Services.SecretKey;
//using Coree.NETASP.UnderConstruction;
//using Coree.NETStandard.Extensions.Validations.String;

//using Microsoft.AspNetCore.Server.IIS.Core;
//using Microsoft.Extensions.Options;

//namespace Coree.NETASP.Middleware.AdditionalHeaders
//{
//    /// <summary>
//    /// Middleware to validate the TFTS (Time Frame Token Service) header token.
//    /// </summary>
//    public class TimeFrameTokenMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly ILogger<TimeFrameTokenMiddleware> _logger;
//        private readonly TimeFrameTokenService _tokenService;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="TimeFrameTokenMiddleware"/> class.
//        /// </summary>
//        /// <param name="next">The next middleware in the pipeline.</param>
//        /// <param name="logger">The logger for this middleware.</param>
//        /// <param name="options">The options for the TimeFrameTokenMiddleware.</param>
//        /// <exception cref="ArgumentNullException">Thrown when options are null.</exception>
//        public TimeFrameTokenMiddleware(RequestDelegate next, ILogger<TimeFrameTokenMiddleware> logger, IOptions<TimeFrameTokenOptions> options)
//        {
//            _next = next;
//            _logger = logger;
//            if (options == null)
//                throw new ArgumentNullException(nameof(options));

//            _tokenService = new TimeFrameTokenService(options.Value);
//        }

//        /// <summary>
//        /// Invokes the middleware to validate the TFTS header token.
//        /// </summary>
//        /// <param name="context">The HTTP context.</param>
//        public async Task InvokeAsync(HttpContext context)
//        {
//            // Retrieve the token from the "TFTS" header and convert it to a string.
//            string tokenValue = context.Request.Headers[_tokenService.HeaderName].ToString();

//            // Check if the token is missing or empty.
//            if (string.IsNullOrWhiteSpace(tokenValue))
//            {
//                _logger.LogError("TFTS header token is missing or empty.");
//                await context.Response.WriteDefaultStatusCodeAnswer(404);
//                return;
//            }

//            // Validate the token using the token service.
//            bool isValid = _tokenService.ValidateToken(tokenValue);

//            if (isValid)
//            {
//                _logger.LogDebug("TFTS validation succeeded; continuing processing.");
//                await _next(context);
//            }
//            else
//            {
//                _logger.LogError("TFTS validation failed; returning 404.");
//                await context.Response.WriteDefaultStatusCodeAnswer(404);
//            }
//        }
//    }

//    /// <summary>
//    /// Extension methods for configuring the TimeFrameTokenMiddleware.
//    /// </summary>
//    public static class TimeFrameTokenServiceExtensions
//    {
//        /// <summary>
//        /// Configures the TimeFrameTokenMiddleware with the specified options.
//        /// </summary>
//        /// <param name="services">The service collection.</param>
//        /// <param name="secret">The secret for the TimeFrameTokenMiddleware.</param>
//        /// <param name="keyChangeInterval">The interval at which the token key changes. Defaults to 1 minute.</param>
//        /// <param name="keyComparisonInterval">The time window during which tokens are considered valid. Defaults to 10 minutes.</param>
//        /// <param name="headerName">The name of the header. Defaults to "TFTS".</param>
//        /// <returns>The updated service collection.</returns>
//        public static IServiceCollection ConfigureTimeFrameTokenServices(
//            this IServiceCollection services,
//            string secret,
//            TimeSpan? keyChangeInterval = null,
//            TimeSpan? keyComparisonInterval = null,
//            string headerName = "TFTS")
//        {
//            services.Configure<TimeFrameTokenOptions>(options =>
//            {
//                options.Secret = secret;
//                options.KeyChangeInterval = keyChangeInterval ?? TimeSpan.FromMinutes(1);
//                options.KeyComparisonInterval = keyComparisonInterval ?? TimeSpan.FromMinutes(10);
//                options.HeaderName = headerName;
//            });

//            return services;
//        }
//    }

//    /// <summary>
//    /// Options for the TimeFrameTokenMiddleware.
//    /// </summary>
//    public class TimeFrameTokenOptions
//    {
//        /// <summary>
//        /// Gets or sets the secret for the TimeFrameTokenMiddleware.
//        /// </summary>
//        public string Secret { get; set; } = "secret";

//        /// <summary>
//        /// Gets or sets the interval at which the token key changes.
//        /// </summary>
//        public TimeSpan KeyChangeInterval { get; set; } = TimeSpan.FromMinutes(1);

//        /// <summary>
//        /// Gets or sets the time window during which tokens are considered valid.
//        /// </summary>
//        public TimeSpan KeyComparisonInterval { get; set; } = TimeSpan.FromMinutes(10);

//        /// <summary>
//        /// Gets or sets the name of the header.
//        /// </summary>
//        public string HeaderName { get; set; } = "TFTS";
//    }

//    public static class HttpClientExtensions
//    {
//        /// <summary>
//        /// Adds an HttpClient named HttpTimeframeTokenClient to the service collection.
//        /// </summary>
//        /// <param name="services">The service collection.</param>
//        /// <param name="baseAddress">The base address for the HttpClient.</param>
//        /// <returns>The updated service collection.</returns>
//        public static IServiceCollection AddHttpTimeframeTokenClient(this IServiceCollection services, Uri baseAddress)
//        {
//            // Add the TimeFrameTokenHandler to the service collection
//            services.AddTransient<TimeFrameTokenHandler>();

//            // Add the HttpClient with the TimeFrameTokenHandler
//            services.AddHttpClient("HttpTimeframeTokenClient", client =>
//            {
//                client.BaseAddress = baseAddress;
//                client.DefaultRequestHeaders.Add("Accept", "application/json");
//            })
//            .AddHttpMessageHandler<TimeFrameTokenHandler>();

//            // Ensure the TimeFrameTokenService is added to the service collection
//            services.AddSingleton(provider =>
//            {
//                var optionsMonitor = provider.GetService<IOptions<TimeFrameTokenOptions>>();
//                if (optionsMonitor == null || optionsMonitor.Value == null)
//                {
//                    throw new InvalidOperationException("TimeFrameTokenOptions are not configured. Please use the ConfigureTimeFrameTokenServices extension method to configure the options.");
//                }

//                TimeFrameTokenOptions options = optionsMonitor.Value;
//                return new TimeFrameTokenService(options);
//            });

//            return services;
//        }
//    }

//    public class TimeFrameTokenHandler : DelegatingHandler
//    {
//        private readonly TimeFrameTokenService _tokenService;

//        public TimeFrameTokenHandler(TimeFrameTokenService tokenService)
//        {
//            _tokenService = tokenService;
//        }

//        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//        {
//            // Generate the current token
//            string token = _tokenService.GenerateCurrentToken();

//            // Add the token to the request headers
//            request.Headers.Add(_tokenService.HeaderName, token);

//            return await base.SendAsync(request, cancellationToken);
//        }
//    }
//}
