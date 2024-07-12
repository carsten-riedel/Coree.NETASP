using System.Collections.Concurrent;

namespace Coree.NETASP.Middleware
{
    public class RequestThrottlingMiddleware2
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestThrottlingMiddleware2> logger;
        private static readonly int MaxValue = int.MaxValue;  // Delay set to 2000 milliseconds (2 seconds)
        private static ConcurrentDictionary<string, SemaphoreSlim> _semaphoreByIP = new ConcurrentDictionary<string, SemaphoreSlim>();
        private static ConcurrentDictionary<string, int> _countByIP = new ConcurrentDictionary<string, int>();

        public RequestThrottlingMiddleware2(ILogger<RequestThrottlingMiddleware2> logger,RequestDelegate next)
        {
            _next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress.ToString();
            // Increment the request count upon receiving a request
            var currentCount = _countByIP.AddOrUpdate(ipAddress, 1, (key, oldValue) => oldValue + 1);

            if (currentCount > MaxValue)
            {
                _countByIP.AddOrUpdate(ipAddress, 0, (key, oldValue) => oldValue > 0 ? oldValue - 1 : 0);
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Too many requests");
                return;
            }

            var semaphore = _semaphoreByIP.GetOrAdd(ipAddress, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                int delayTime = CalculateDelay(currentCount);
                logger.LogDebug("RequestThrottling: Count: {currentCount} Delay: {delayTime} ms", currentCount, delayTime);
                await Task.Delay(delayTime);
                _countByIP.AddOrUpdate(ipAddress, 0, (key, oldValue) => oldValue > 0 ? oldValue - 1 : 0);
                await _next(context);  // Process the request

                // Decrement the count after the request has been processed successfully
                
            }
            finally
            {
                semaphore.Release();
            }
        }

        // Helper method to calculate the delay based on the current request count
        private int CalculateDelay(int currentCount)
        {
            if (currentCount >= 100)
                return 5000;
            if (currentCount >= 50)
                return 2500;
            if (currentCount >= 35)
                return 1500;
            if (currentCount >= 30)
                return 1250;
            if (currentCount >= 25)
                return 1000;
            if (currentCount >= 20)
                return 750;
            if (currentCount >= 15)
                return 500;
            if (currentCount >= 10)
                return 250;
            if (currentCount >= 5)
                return 100;
            if (currentCount >= 3)
                return 75;
            return 50; // No delay for less than 10 requests
        }
    }

}