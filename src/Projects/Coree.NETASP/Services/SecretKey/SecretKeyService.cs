//using System.Collections.Generic;
//using System.Security.Cryptography;
//using System.Text;
//using System;
//using Coree.NETASP.Middleware.AdditionalHeaders;

//namespace Coree.NETASP.Services.SecretKey
//{
//    /// <summary>
//    /// Provides token generation and validation based on time intervals and a shared secret.
//    /// </summary>
//    /// <remarks>
//    /// This service uses a fixed salt and generates tokens using HMAC-SHA256. The parameters
//    /// (shared secret and time intervals) can be updated at runtime using the UpdateParameters method.
//    /// </remarks>
//    public class TimeFrameTokenService
//    {
//        private const string FixedSalt = "09BB2E93-0F13-4204-9726-CCDEE9C8DC4B";
//        private string _sharedSecret;
//        private TimeSpan _keyChangeInterval;
//        private TimeSpan _keyComparisonInterval;
//        private string _headerName;

//        /// <summary>
//        /// Gets the name of the header.
//        /// </summary>
//        public string HeaderName => _headerName;

//        /// <summary>
//        /// Initializes a new instance of the TimeFrameTokenService class.
//        /// </summary>
//        /// <param name="options">The options for the TimeFrameTokenService.</param>
//        public TimeFrameTokenService(TimeFrameTokenOptions options)
//        {
//            if (options == null)
//            {
//                throw new ArgumentNullException(nameof(options));
//            }
//            if (string.IsNullOrEmpty(options.Secret))
//            {
//                throw new ArgumentException("Shared secret must not be null or empty", nameof(options.Secret));
//            }
//            _sharedSecret = options.Secret;
//            _keyChangeInterval = options.KeyChangeInterval;
//            _keyComparisonInterval = options.KeyComparisonInterval;
//            _headerName = options.HeaderName;
//        }

//        /// <summary>
//        /// Updates the parameters (shared secret, time intervals, and header name) for token generation.
//        /// </summary>
//        /// <param name="sharedSecret">The new shared secret to use.</param>
//        /// <param name="keyChangeInterval">
//        /// The new key change interval. If null, defaults to 10 seconds.
//        /// </param>
//        /// <param name="keyComparisonInterval">
//        /// The new key comparison interval. If null, defaults to 60 seconds.
//        /// </param>
//        /// <param name="headerName">The new header name to use. If null, defaults to "TFTS".</param>
//        /// <example>
//        /// <code>
//        /// // Update the token service parameters
//        /// tokenService.UpdateParameters("newSecret", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(120), "NewHeader");
//        /// </code>
//        /// </example>
//        public void UpdateParameters(string sharedSecret, TimeSpan? keyChangeInterval = null, TimeSpan? keyComparisonInterval = null, string headerName = "TFTS")
//        {
//            if (string.IsNullOrEmpty(sharedSecret))
//            {
//                throw new ArgumentException("Shared secret must not be null or empty", nameof(sharedSecret));
//            }
//            _sharedSecret = sharedSecret;
//            _keyChangeInterval = keyChangeInterval ?? TimeSpan.FromSeconds(10);
//            _keyComparisonInterval = keyComparisonInterval ?? TimeSpan.FromSeconds(60);
//            _headerName = headerName;
//        }

//        /// <summary>
//        /// Generates and returns the current token for the active time interval.
//        /// </summary>
//        /// <returns>The current token as a hexadecimal string.</returns>
//        public string GenerateCurrentToken()
//        {
//            // Use the current UTC time.
//            string timeFactor = ComposeTimeFactor(DateTime.UtcNow);
//            return ComputeHmacSignature(timeFactor);
//        }

//        /// <summary>
//        /// Validates whether the provided token matches any token in the valid time window.
//        /// </summary>
//        /// <param name="token">The token to validate.</param>
//        /// <returns>True if the token is valid; otherwise, false.</returns>
//        public bool ValidateToken(string token)
//        {
//            if (string.IsNullOrEmpty(token))
//                return false;

//            // Calculate the number of intervals in the key comparison window.
//            int count = (int)Math.Ceiling((double)_keyComparisonInterval.Ticks / _keyChangeInterval.Ticks);
//            foreach (var validToken in GenerateTokenWindow(count))
//            {
//                if (string.Equals(validToken, token, StringComparison.OrdinalIgnoreCase))
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        /// <summary>
//        /// Generates a series of tokens covering the valid time window.
//        /// </summary>
//        /// <param name="count">
//        /// The number of tokens to generate, corresponding to the number of key-change intervals in the comparison window.
//        /// </param>
//        /// <returns>An enumerable of valid tokens.</returns>
//        private IEnumerable<string> GenerateTokenWindow(int count)
//        {
//            if (count < 1)
//                throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

//            var tokens = new List<string>();
//            DateTime utcNow = DateTime.UtcNow;
//            // Truncate the current time using the key-change interval.
//            DateTime currentTruncatedTime = new DateTime(utcNow.Ticks - (utcNow.Ticks % _keyChangeInterval.Ticks), DateTimeKind.Utc);

//            for (int i = 0; i < count; i++)
//            {
//                DateTime intervalTime = currentTruncatedTime.AddTicks(-_keyChangeInterval.Ticks * i);
//                // Use the ComposeTimeFactor helper to build the token input.
//                string timeFactor = ComposeTimeFactor(intervalTime);
//                tokens.Add(ComputeHmacSignature(timeFactor));
//            }

//            return tokens;
//        }

//        /// <summary>
//        /// Composes the time-based input by truncating the provided time to the key-change interval and appending a fixed salt.
//        /// </summary>
//        /// <param name="time">The time to use when generating the input factor.</param>
//        /// <returns>A string representing the time factor.</returns>
//        private string ComposeTimeFactor(DateTime time)
//        {
//            DateTime truncatedTime = new DateTime(time.Ticks - (time.Ticks % _keyChangeInterval.Ticks), DateTimeKind.Utc);
//            return truncatedTime.Ticks.ToString() + FixedSalt;
//        }

//        /// <summary>
//        /// Computes the HMAC-SHA256 signature for the given message using the shared secret.
//        /// </summary>
//        /// <param name="message">The message to sign.</param>
//        /// <returns>A hexadecimal string representation of the HMAC signature.</returns>
//        private string ComputeHmacSignature(string message)
//        {
//            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_sharedSecret)))
//            {
//                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
//                var sb = new StringBuilder(hashBytes.Length * 2);
//                foreach (byte b in hashBytes)
//                {
//                    sb.Append(b.ToString("x2"));
//                }
//                return sb.ToString();
//            }
//        }
//    }
//}