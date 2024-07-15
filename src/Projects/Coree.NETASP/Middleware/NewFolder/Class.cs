namespace Coree.NETASP.Middleware.NewFolder
{
    public class PreMiddlewareHostedService : IHostedService
    {
        private readonly TaskCompletionSource<bool> _startupCompleted = new TaskCompletionSource<bool>();

        /// <summary>
        /// Gets a task that completes when the startup process is done.
        /// </summary>
        public Task StartupCompleted => _startupCompleted.Task;

        /// <summary>
        /// Executes the background service task.
        /// </summary>
        /// <param name="stoppingToken">Token that indicates when to stop the task.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            // Placeholder for your implementation
            // Perform initialization or pre-middleware tasks here

            // Simulate some startup task
            await Task.Delay(1000, stoppingToken);

            // Signal that startup has completed
            _startupCompleted.SetResult(true);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            // Placeholder for any cleanup tasks
            return Task.CompletedTask;
        }
    }

    public class WaitForPreMiddlewareHostedServiceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PreMiddlewareHostedService _preMiddlewareHostedService;

        public WaitForPreMiddlewareHostedServiceMiddleware(RequestDelegate next, PreMiddlewareHostedService preMiddlewareHostedService)
        {
            _next = next;
            _preMiddlewareHostedService = preMiddlewareHostedService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _preMiddlewareHostedService.StartupCompleted;
            await _next(context);
        }
    }

    public static class fooExtensions
    {
        public static IServiceCollection AddPreMiddlewareHostedService(this IServiceCollection services)
        {
            // Register as both a singleton for direct retrieval and as a hosted service
            services.AddSingleton<PreMiddlewareHostedService>();
            services.AddHostedService(sp => sp.GetRequiredService<PreMiddlewareHostedService>());

            return services;
        }

        /// <summary>
        /// Adds the PreMiddlewareHostedService to the service collection and configures the middleware.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UsePreMiddlewareHostedService(this WebApplication app)
        {
            // Get the PreMiddlewareHostedService from the service collection
            var preMiddlewareHostedService = app.Services.GetRequiredService<PreMiddlewareHostedService>();

            // Use the middleware to wait for the hosted service to complete startup
            app.UseMiddleware<WaitForPreMiddlewareHostedServiceMiddleware>(preMiddlewareHostedService);

            return app;
        }
    }
}