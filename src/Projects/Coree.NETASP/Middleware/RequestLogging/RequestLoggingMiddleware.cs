using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;

namespace Coree.NETASP.Middleware.RequestLogging
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RequestLoggingMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for capturing log outputs.</param>
        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Processes a request to log its URL and headers.
        /// </summary>
        /// <param name="context">HTTP context for the current request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var result = await InvokeRequestTasksAsync(context);
            var startTime = DateTime.UtcNow;
            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OperationCanceledException");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while processing the response logging.");
                // Handle or rethrow the exception as needed
            }
            await InvokeResponseTasksAsync(context, startTime, result);
        }

        private Task<bool> InvokeRequestTasksAsync(HttpContext context)
        {
            // List of User-Agent substrings to ignore
            var ignoredUserAgentList = new List<string> { "YARP/" };

            // Check if User-Agent header is present
            bool isIgnored = false;
            if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
            {
                // Check if the User-Agent contains any of the substrings in the ignored list
                isIgnored = ignoredUserAgentList.Any(ignored => userAgent.ToString().Contains(ignored, StringComparison.OrdinalIgnoreCase));
            }


            // Check if User-Agent header is present and if its value is in the ignored list
            if (!isIgnored)
            {
                //var request = context.Request;
                //var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

                var RemoteIP = context.Connection.RemoteIpAddress?.ToString();
                var RemotePort = context.Connection.RemotePort.ToString();

                // Log request URL and headers if the User-Agent is not in the ignored list
                StringBuilder requestString = new StringBuilder();
                requestString.AppendLine();
                requestString.AppendLine($"Connection.Id: {context.Connection.Id}");
                requestString.AppendLine($"Trace:         {context.TraceIdentifier}");
                requestString.AppendLine($"Request:       {context.Request.GetDisplayUrl()}");
                requestString.AppendLine($"Method:        {context.Request.Method}");
                requestString.AppendLine($"Remote:        {RemoteIP}:{RemotePort}");
                //_logger.LogInformation("-- Request URL: {URL}, Client IP: {ClientIP}", context.Request.GetDisplayUrl(), clientIP);

                if (RemoteIP != null)
                {
                    try
                    {
                        var dns = Dns.GetHostEntry(RemoteIP);
                        requestString.AppendLine($"RemoteHost:    {dns.HostName}");
                    }
                    catch (Exception)
                    {
                        requestString.AppendLine($"RemoteHost:    Unresolvable");
                    }

                }

                foreach (var header in context.Request.Headers)
                {
                    requestString.AppendLine($"Header:        {header.Key} = {header.Value}");
                }
                //_logger.LogInformation("Header: {Key}: {Value}", header.Key, header.Value);
                _logger.LogInformation("Request: {request}", requestString.ToString());
            }
            return Task.FromResult(isIgnored);
        }

        private Task InvokeResponseTasksAsync(HttpContext context, DateTime startTime, bool isignored)
        {
            if (!isignored)
            {
                context.Response.OnCompleted(async () =>
                {
                    try
                    {
                        // Calculate the response time
                        var duration = DateTime.UtcNow - startTime;

                        StringBuilder responseString = new StringBuilder();
                        responseString.AppendLine();
                        responseString.AppendLine($"Connection id: {context.Connection.Id}");
                        responseString.AppendLine($"Request id:    {context.TraceIdentifier}");
                        responseString.AppendLine($"StatusCode:    {context.Response.StatusCode}");
                        responseString.AppendLine($"Duration:      {duration.TotalMilliseconds}");
                        responseString.AppendLine($"ContentType:   {context.Response.ContentType}");

                        foreach (var header in context.Response.Headers)
                        {
                            responseString.AppendLine($"Header:        {header.Key} = {header.Value}");
                        }
                        _logger.LogInformation("Response:      {response}", responseString.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An exception occurred while processing the response logging after completion.");
                    }
                });
            }

            return Task.CompletedTask;
        }
    }
}