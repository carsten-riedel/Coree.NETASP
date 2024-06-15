namespace Coree.NETASP.Extensions.WebApplicationBuilderExtensions
{
    public static partial class WebApplicationBuilderExtension
    {
        /// <summary>
        /// Dumps the configuration settings of the ASP.NET Core application to the console.
        /// This includes host filtering, Kestrel server options, CORS policies, authentication schemes, and cookie policy settings.
        /// </summary>
        /// <remarks>
        /// Use this method to quickly visualize configuration values during development.
        /// Note: This method should be called only in a debug build due to performance considerations and the potential sensitivity of the configuration data.
        /// </remarks>
        /// <param name="builder">The WebApplicationBuilder used to configure the application.</param>
        public static void DumpAspNetCoreConfiguration(this WebApplicationBuilder builder)
        {
#if DEBUG
    using (var serviceProvider = builder.Services.BuildServiceProvider())
    {
        // Dump HostFilteringOptions
        var hostFilterOptions = serviceProvider.GetService<IOptions<HostFilteringOptions>>();
        if (hostFilterOptions != null)
        {
            Console.WriteLine($"AllowedHosts: {string.Join(", ", hostFilterOptions.Value.AllowedHosts)}");
            Console.WriteLine($"AllowEmptyHosts: {hostFilterOptions.Value.AllowEmptyHosts}");
        }

        // Dump Kestrel Server Options
        var kestrelServerOptions = serviceProvider.GetService<IOptions<KestrelServerOptions>>();
        if (kestrelServerOptions != null)
        {
            Console.WriteLine($"Kestrel: MaxConcurrentConnections: {kestrelServerOptions.Value.Limits.MaxConcurrentConnections}");
            Console.WriteLine($"Kestrel: MaxConcurrentUpgradedConnections: {kestrelServerOptions.Value.Limits.MaxConcurrentUpgradedConnections}");
        }

        // Dump CORS policy settings
        var corsOptions = serviceProvider.GetService<IOptions<CorsOptions>>();
        if (corsOptions != null)
        {
            foreach (var policy in corsOptions.Value.GetPolicies())
            {
                Console.WriteLine($"CORS Policy {policy.Key}: {string.Join(", ", policy.Value.Origins)}");
            }
        }

        // Dump Authentication schemes
        var authOptions = serviceProvider.GetService<IOptions<AuthenticationOptions>>();
        if (authOptions != null)
        {
            foreach (var scheme in authOptions.Value.Schemes)
            {
                Console.WriteLine($"Authentication Scheme: {scheme.Name}");
            }
        }

        // Dump Application Cookie settings
        var cookieOptions = serviceProvider.GetService<IOptions<CookiePolicyOptions>>();
        if (cookieOptions != null)
        {
            Console.WriteLine($"Cookie Policy - MinimumSameSitePolicy: {cookieOptions.Value.MinimumSameSitePolicy}");
        }
    }
#endif
        }
    }
}
