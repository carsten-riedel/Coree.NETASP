namespace Coree.NETASP.Middleware.AcceptLanguageFiltering
{
    public static class AcceptLanguageFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddAcceptLanguageFiltering(this IServiceCollection services, string[]? whitelist =null, string[]? blacklist = null)
        {
            services.Configure<AcceptLanguageFilteringOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
            });

            return services;
        }
    }
}
