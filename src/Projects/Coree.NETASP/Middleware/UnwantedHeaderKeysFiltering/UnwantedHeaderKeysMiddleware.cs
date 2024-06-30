﻿using Coree.NETASP.Extensions.HttpResponsex;
using Coree.NETStandard.Extensions.Validations.String;

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
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
                var isValid = header.Key.ValidateWhitelistBlacklist(null,_UnwantedHeaderKeysOptions.Blacklist?.ToList());
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

        private static string GetStatusCodeDescription(int statusCode)
        {
            return statusCode switch
            {
                100 => "Continue",
                101 => "Switching Protocols",
                102 => "Processing",
                103 => "Early Hints",
                200 => "OK",
                201 => "Created",
                202 => "Accepted",
                203 => "Non-Authoritative Information",
                204 => "No Content",
                205 => "Reset Content",
                206 => "Partial Content",
                207 => "Multi-Status",
                208 => "Already Reported",
                226 => "IM Used",
                300 => "Multiple Choices",
                301 => "Moved Permanently",
                302 => "Found",
                303 => "See Other",
                304 => "Not Modified",
                307 => "Temporary Redirect",
                308 => "Permanent Redirect",
                400 => "Bad Request",
                401 => "Unauthorized",
                402 => "Payment Required",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                406 => "Not Acceptable",
                407 => "Proxy Authentication Required",
                408 => "Request Timeout",
                409 => "Conflict",
                410 => "Gone",
                411 => "Length Required",
                412 => "Precondition Failed",
                413 => "Payload Too Large",
                414 => "URI Too Long",
                415 => "Unsupported Media Type",
                416 => "Range Not Satisfiable",
                417 => "Expectation Failed",
                418 => "I'm a teapot",
                421 => "Misdirected Request",
                422 => "Unprocessable Entity",
                423 => "Locked",
                424 => "Failed Dependency",
                425 => "Too Early",
                426 => "Upgrade Required",
                428 => "Precondition Required",
                429 => "Too Many Requests",
                431 => "Request Header Fields Too Large",
                451 => "Unavailable For Legal Reasons",
                500 => "Internal Server Error",
                501 => "Not Implemented",
                502 => "Bad Gateway",
                503 => "Service Unavailable",
                504 => "Gateway Timeout",
                505 => "HTTP Version Not Supported",
                506 => "Variant Also Negotiates",
                507 => "Insufficient Storage",
                508 => "Loop Detected",
                510 => "Not Extended",
                511 => "Network Authentication Required",
                _ => "Unknown Status Code"
            };
        }
    }

    public static class UnwantedHeaderKeysMiddlewareExtensions
    {


        public static IServiceCollection AddUnwantedHeaderKeysFiltering(this IServiceCollection services,string[]? blacklist = null)
        {

            services.Configure<UnwantedHeaderKeysOptions>(options =>
            {
                options.Blacklist = blacklist;
            });

            return services;
        }

   


    }
}
