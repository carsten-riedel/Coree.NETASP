using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Extensions.Options.KestrelServer
{
    public static partial class OptionsKestrelServerOptionsExtensions
    {
        public static void dd(this KestrelServerOptions kestrelServerOptions)
        {
            kestrelServerOptions.ListenAnyIP(80);
        }


        public static void AspNetCoreDump(this WebApplicationBuilder builder)
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
