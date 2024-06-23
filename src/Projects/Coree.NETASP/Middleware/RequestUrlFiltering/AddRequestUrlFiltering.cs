namespace Coree.NETASP.Middleware.RequestUrlFiltering
{
    public static class RequestUrlFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddRequestUrlFiltering(this IServiceCollection services, string[]? whitelist =null, string[]? blacklist = null)
        {
            services.Configure<RequestUrlFilteringOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
            });

            return services;
        }
    }
}
