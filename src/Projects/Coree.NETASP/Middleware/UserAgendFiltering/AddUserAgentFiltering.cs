namespace Coree.NETASP.Middleware.UserAgentFiltering
{
    public static class UserAgentFilteringExtensions
    {
        /// <summary>
        /// Adds and configures the HostNameFilteringMiddleware options.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddUserAgentFiltering(this IServiceCollection services, string[]? whitelist =null, string[]? blacklist = null)
        {
            services.Configure<UserAgentFilterOptions>(options =>
            {
                options.Whitelist = whitelist;
                options.Blacklist = blacklist;
            });

            return services;
        }
    }
}
