using Microsoft.Extensions.Caching.Memory;

namespace Coree.NETASP.Middleware.PathDeep
{
    public class MaliciousRequestBlockerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MemoryCache _ipBlockList;
        private readonly TimeSpan _blockDuration;
        private readonly ILogger<MaliciousRequestBlockerMiddleware> _logger;
        private readonly int _blockScoreThreshold;

        public MaliciousRequestBlockerMiddleware(RequestDelegate next, ILogger<MaliciousRequestBlockerMiddleware> logger,
                                                  int blockDurationInSeconds = 300)
        {
            _next = next;
            _logger = logger;
            _blockDuration = TimeSpan.FromSeconds(blockDurationInSeconds);
            _ipBlockList = new MemoryCache(new MemoryCacheOptions());
            _blockScoreThreshold = 100;  // Total score that triggers a block
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            var requestPath = context.Request.Path.ToString();
            var pathDepth = CalculatePathDepth(requestPath);
            var requestKey = remoteIp;  // Track by IP only

            // Calculate score based on path depth
            int scoreIncrement = CalculateScoreIncrement(pathDepth);
            var currentScore = _ipBlockList.GetOrCreate(requestKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _blockDuration;
                return scoreIncrement;  // Initialize score
            });

            if (currentScore >= _blockScoreThreshold)
            {
                _ipBlockList.Set(requestKey, currentScore, _blockDuration);
                _logger.LogError($"IP Temporarily Blocked: {remoteIp} with a total score of {currentScore}. Triggered by suspicious path depth activity.");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Forbidden: Not allowed.");
                return;
            }
            else
            {
                _ipBlockList.Set(requestKey, currentScore + scoreIncrement, _blockDuration);
                _logger.LogInformation($"Updated score for {remoteIp}: {currentScore + scoreIncrement}. Current Path Depth: {pathDepth}");
            }

            await _next(context);
        }

        private int CalculatePathDepth(string path)
        {
            var normalizedPath = TrimSpecific(path, '/', 1, 1);
            if (String.IsNullOrEmpty(normalizedPath))
            {
                return 0;
            }
            var res = normalizedPath.Split('/', StringSplitOptions.None);
            return res.Length;
        }

        private int CalculateScoreIncrement(int pathDepth)
        {
            // Define score increments based on path depth
            switch (pathDepth)
            {
                case 0:
                    return 0;

                case 1:
                    return 0; // Shallow paths are less suspicious
                case 2:
                    return 0; // Slightly more suspicious
                case 3:
                    return 100; // Even more suspicious
                default:
                    return 100; // Very deep paths are highly suspicious
            }
        }

        public string TrimSpecific(string input, char charToTrim, int countFromStart, int countFromEnd)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int start = 0;
            int end = input.Length;

            // Trim from start
            while (start < end && input[start] == charToTrim && countFromStart > 0)
            {
                start++;
                countFromStart--;
            }

            // Trim from end
            while (end > start && input[end - 1] == charToTrim && countFromEnd > 0)
            {
                end--;
                countFromEnd--;
            }

            if (start >= end)
                return string.Empty;

            return input.Substring(start, end - start);
        }
    }
}
