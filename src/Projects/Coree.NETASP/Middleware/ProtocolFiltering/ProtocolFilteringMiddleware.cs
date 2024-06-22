using Microsoft.Extensions.Options;

using static System.Net.WebRequestMethods;

namespace Coree.NETASP.Middleware.ProtocolFiltering
{
    [Flags]
    public enum Protocols
    {
        None = 0,
        Http10 = 1,
        Http11 = 2,
        Http20 = 4,
        Http30 = 8 
    }

    public class ProtocolFilteringOptions
    {
        public Protocols AllowedProtocols { get; set; } = Protocols.Http11 | Protocols.Http20 | Protocols.Http30;
    }

    /// <summary>
    /// Middleware to filter requests based on the HTTP protocol used.
    /// </summary>
    public class ProtocolFilteringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProtocolFilteringMiddleware> _logger;
        private readonly ProtocolFilteringOptions _protocolFilteringOptions;
        private readonly List<string> _allowedProtocols;

        public ProtocolFilteringMiddleware(RequestDelegate next, ILogger<ProtocolFilteringMiddleware> logger, IOptions<ProtocolFilteringOptions> options)
        {
            _next = next;
            _logger = logger;
            _protocolFilteringOptions = options.Value;
            _allowedProtocols = _protocolFilteringOptions.AllowedProtocols.ToProtocolString();
        }

        /// <summary>
        /// Invoke method to process the HTTP context based on allowed protocols.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string protocol = context.Request.Protocol;
            
            var isInList = _allowedProtocols.Contains(protocol);
            // Check if the protocol is allowed.
            if (!isInList)
            {
                _logger.LogError("Protocol: '{Protocol}' is not allowed.", protocol);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Forbidden: Not allowed.");
                return;
            }

            _logger.LogDebug("Protocol: '{Protocol}' is  allowed.", protocol);
            
            await _next(context);
        }
    }

    public static class ProtocolFilteringMiddlewareExtensions
    {
        public static readonly Dictionary<Protocols, string> ProtocolStrings = new Dictionary<Protocols, string>
        {
            {Protocols.Http10, "HTTP/1.0"},
            {Protocols.Http11, "HTTP/1.1"},
            {Protocols.Http20, "HTTP/2.0"},
            {Protocols.Http30, "HTTP/3.0"},

        };

        /// <summary>
        /// Converts a Protocols enum value to a list of string representations of allowed protocols.
        /// </summary>
        /// <param name="protocols">The protocol enum value.</param>
        /// <returns>A list of string representations of the protocols.</returns>
        public static List<string> ToProtocolString(this Protocols protocols)
        {
            var strings = new List<string>();
            foreach (var protocol in ProtocolStrings)
            {
                if (protocols.HasFlag(protocol.Key))
                {
                    strings.Add(protocol.Value);
                }
            }
            return strings;
        }

        public static IServiceCollection AddProtocolFiltering(this IServiceCollection services, Protocols protocols = Protocols.Http11 | Protocols.Http20)
        {
            services.Configure<ProtocolFilteringOptions>(options =>
            {
                options.AllowedProtocols = protocols;
            });

            return services;
        }
    }
}
