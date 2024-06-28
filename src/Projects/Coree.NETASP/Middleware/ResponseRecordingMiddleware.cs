namespace Coree.NETASP.Middleware
{
    public class ResponseRecordingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseRecordingMiddleware> _logger;

        public ResponseRecordingMiddleware(RequestDelegate next, ILogger<ResponseRecordingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using (var responseBodyStream = new MemoryStream())
            {
                context.Response.Body = responseBodyStream;

                await _next(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                _logger.LogInformation($"Response: {responseBodyText}");

                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
        }
    }
}
