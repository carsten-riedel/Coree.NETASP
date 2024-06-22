namespace Coree.NETASP.Middleware.HostNameFiltering
{
    public static class HostNameFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddHostNameFiltering(this IServiceCollection services, string[] allowedHosts)
        {
            services.Configure<HostNameFilterOptions>(options =>
            {
                options.AllowedHosts = allowedHosts;
            });

            return services;
        }
    }
}
