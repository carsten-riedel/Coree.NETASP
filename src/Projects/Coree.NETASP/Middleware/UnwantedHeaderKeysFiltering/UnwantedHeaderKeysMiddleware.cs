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
                var isValid = header.Key.ValidateWhitelistBlacklist(null,_UnwantedHeaderKeysOptions.Blacklist);
                if (!isValid)
                {
                    _logger.LogError("Access denied due to unwanted header key: {Key}", header.Key);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Forbidden: Not allowed.");
                    return;
                }
            }

            _logger.LogDebug("No unwanted headers detected. Proceeding with request.");
            await _next(context);
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

        /// <summary>
        /// Attempts to match the given input against the specified pattern using regular expressions.
        /// Returns true if the input matches the pattern, false otherwise.
        /// </summary>
        /// <param name="input">The input string to be matched.</param>
        /// <param name="pattern">The pattern string, where '*' matches any number of characters and '?' matches one.</param>
        /// <returns>True if the input matches the pattern; otherwise, false.</returns>
        private static bool IsRegExMatch(string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                return input == null && pattern == null;
            }

            int minInputLength = pattern.Replace("*", "").Replace("?", "").Length;

            if (input.Length < minInputLength)
            {
                return false;
            }

            var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".?") + "$";
            var isMatch = Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
            return isMatch;
        }

        /// <summary>
        /// Checks if the input matches any of the provided patterns.
        /// </summary>
        /// <param name="input">The input string to be matched.</param>
        /// <param name="patterns">A list of patterns to match against the input.</param>
        /// <returns>True if any pattern matches the input; otherwise, false.</returns>
        private static bool MatchesAnyPattern(string? input, List<string>? patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return false;
            }

            foreach (var pattern in patterns)
            {
                if (IsRegExMatch(input, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extension method to evaluate this string instance against optional whitelist and blacklist patterns to determine
        /// whether the operation should continue.
        /// </summary>
        /// <param name="input">The string instance to be evaluated.</param>
        /// <param name="whitelist">An optional list of whitelist patterns. If the string matches any of these patterns,
        /// the method immediately returns true, indicating that the operation should continue.</param>
        /// <param name="blacklist">An optional list of blacklist patterns. If the string matches any of these patterns,
        /// the method returns false, indicating that the operation should be halted.</param>
        /// <returns>True if the operation should continue (either by passing a whitelist check or not failing a blacklist check),
        /// or false if the string matches a blacklist pattern and should be halted.</returns>
        public static bool ValidateWhitelistBlacklist(this string input, IEnumerable<string>? whitelist = null, IEnumerable<string>? blacklist = null)
        {
            // Utilize the ValidateWhitelistBlacklist method directly
            return ValidationWhitelistBlacklist(input, whitelist?.ToList(), blacklist?.ToList());
        }

        /// <summary>
        /// Evaluates an input string against optional whitelist and blacklist to determine whether the operation should continue.
        /// This method returns true to signal that the operation should continue, either because the input matches a whitelist
        /// pattern, does not match a blacklist pattern, or no lists are provided.
        /// </summary>
        /// <param name="input">The input string to be evaluated.</param>
        /// <param name="whitelist">An optional list of whitelist patterns. If the input matches any of these patterns,
        /// the function immediately returns true, indicating that the operation should continue.</param>
        /// <param name="blacklist">An optional list of blacklist patterns. If the input matches any of these patterns,
        /// the function returns false, indicating that the operation should be halted.</param>
        /// <returns>True if the operation should continue (either by passing a whitelist check or not failing a blacklist check),
        /// or false if the input matches a blacklist pattern and should be halted.</returns>
        private static bool ValidationWhitelistBlacklist(string input, List<string>? whitelist = null, List<string>? blacklist = null)
        {
            // Check against the whitelist if it is provided
            if (whitelist != null)
            {
                var whitelistResult = MatchesAnyPattern(input, whitelist);
                if (whitelistResult)
                {
                    // The input matches a whitelist pattern; continue the operation
                    return true;
                }
            }

            // If the input does not match any whitelist pattern or no whitelist is provided, check the blacklist
            if (blacklist != null)
            {
                var blacklistResult = MatchesAnyPattern(input, blacklist);
                // If the input matches a blacklist pattern, halt the operation
                return !blacklistResult;
            }

            // If no lists are provided or no matches found in blacklist, continue the operation
            return true;
        }

    }
}
