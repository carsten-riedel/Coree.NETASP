using Coree.NETASP.Extensions.HttpResponsex;
using Coree.NETASP.Services.Points;

namespace Coree.NETASP.Middleware
{
    public class FailurePointsMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;
        private readonly ILogger<FailurePointsMiddleware> _logger;
        private readonly IPointService _pointService;

        public FailurePointsMiddleware(RequestDelegate next, ILogger<FailurePointsMiddleware> logger, IPointService pointService)
        {
            this._nextMiddleware = next;
            this._logger = logger;
            this._pointService = pointService;
        }

        /// <summary>
        /// Invoke method to process the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            string? requestIp = context.Connection.RemoteIpAddress?.ToString();
            if (requestIp == null)
            {
                _logger.LogError("Request Ip: Request without IPs are not allowed.");
                await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status422UnprocessableEntity);
                return;
            }

            Entry? entrys = _pointService.GetPoints(requestIp);

            if (entrys == null)
            {
                await _nextMiddleware(context);
                return;
            }

            var points = entrys.PointEntries.Sum(e => e.Points);
            
            if (points == 0)
            {
                await _nextMiddleware(context);
                return;
            }
            else
            {
                _logger.LogError("FailurePoints: Request Ip: {requestIp} rejected cause of {points} failure points.(History)", requestIp,points);
                await context.Response.WriteDefaultStatusCodeAnswer(StatusCodes.Status400BadRequest);
                return;
            }

            
        }
    }
}