﻿using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;


namespace Coree.NETASP.Extensions.KestrelOptions
{
    public static class ConfigureWebHostBuilderExtensions
    {
        public enum ListenType
        {
            Localhost,
            AnyIp,
        }

        public static void ConfigureKestrelSimple(this ConfigureWebHostBuilder configureWebHostBuilder, Dictionary<string, X509Certificate2> certs, int? httpPort = 80, int? httpsPort = 443, ListenType listenType = ListenType.Localhost, bool useOutDateProtocols = false)
        {
            if ((!httpPort.HasValue) && (!httpPort.HasValue))
            {
                throw new ArgumentException("");
            }

            configureWebHostBuilder.ConfigureKestrel(serverOptions =>
            {
                serverOptions.AddServerHeader = false;

                if (listenType == ListenType.Localhost)
                {
                    if (httpPort.HasValue)
                    {
                        serverOptions.ListenLocalhost(httpPort.Value, listenOptions => { });
                    }

                    if (httpsPort.HasValue)
                    {
                        serverOptions.ListenLocalhost(httpsPort.Value, listenOptions =>
                        {
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ServerCertificateSelector = (connectionContext, sni) =>
                                {
                                    if (sni is not null)
                                    {
                                        var cert = certs.FirstOrDefault(e => sni.EndsWith(e.Key, StringComparison.OrdinalIgnoreCase));
                                        if (cert.Value != null)
                                        {
                                            return cert.Value;
                                        }
                                    }

                                    return certs.Last().Value;
                                };
                            });
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                    }
                }
                else if (listenType == ListenType.AnyIp)
                {
                    if (httpPort.HasValue)
                    {
                        serverOptions.ListenAnyIP(httpPort.Value, listenOptions => { });
                    }

                    if (httpsPort.HasValue)
                    {
                        serverOptions.ListenAnyIP(httpsPort.Value, listenOptions =>
                        {
                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ServerCertificateSelector = (connectionContext, sni) =>
                                {
                                    if (sni is not null)
                                    {
                                        var cert = certs.FirstOrDefault(e => sni.EndsWith(e.Key, StringComparison.OrdinalIgnoreCase));
                                        if (cert.Value != null)
                                        {
                                            return cert.Value;
                                        }
                                    }

                                    return certs.Last().Value;
                                };
                            });
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        });
                    }
                }

                if (useOutDateProtocols)
                {
                    serverOptions.ConfigureHttpsDefaults(config =>
                    {
                        config.SslProtocols = System.Security.Authentication.SslProtocols.Ssl2 |
                                System.Security.Authentication.SslProtocols.Ssl3 |
                                System.Security.Authentication.SslProtocols.Tls |
                                System.Security.Authentication.SslProtocols.Tls11 |
                                System.Security.Authentication.SslProtocols.Tls12 |
                                System.Security.Authentication.SslProtocols.Tls13;
                    });
                }
            });
        }
    }

    public static class xExtensions
    {
        public static IServiceCollection AddDefaultResponseCompression(this IServiceCollection serviceDescriptors)
        {
            return serviceDescriptors.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();

                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                        "image/svg+xml",
                        "application/json",
                        "application/xml",
                        "text/css",
                        "application/javascript",
                        "text/html",
                        "text/xml",
                        "text/plain",
                        "application/vnd.ms-fontobject",
                        "font/eot",
                        "font/opentype",
                        "font/otf",
                        "image/bmp",
                        "image/x-icon",
                        "text/javascript",
                        "application/x-javascript",
                        "text/js",
                        "application/atom+xml",
                        "application/rss+xml",
                        "application/vnd.api+json",
                        "application/x-web-app-manifest+json",
                        "text/markdown",
                        "application/manifest+json",
                        "application/x-font-ttf",
                        "font/ttf",
                        "font/woff",
                        "font/woff2",
                        "application/font-woff",
                        "application/font-woff2",
                        "image/vnd.microsoft.icon"
                    });
            });
        }
    }

    public static class AuthenticationBuilderExtensions
    {

        private static async Task<IEnumerable<Claim>> GetUserClaimsAsync(string userId)
        {
            throw new NotImplementedException();
        }

        private static bool HaveClaimsChanged(IEnumerable<Claim> currentClaims, IEnumerable<Claim> newClaims)
        {
            // Compare current claims with new claims
            // This is a simple example; you might need a more sophisticated comparison
            var currentClaimsSet = new HashSet<Claim>(currentClaims);
            var newClaimsSet = new HashSet<Claim>(newClaims);

            return !currentClaimsSet.SetEquals(newClaimsSet);
        }

        public static AuthenticationBuilder AddDefaultCookieConfig(this AuthenticationBuilder builder)
        {

            return builder.AddDefaultCookieConfig(
                contextOnSignedIn =>
                {
                    return Task.CompletedTask;
                },
                contextOnRedirectToAccessDenied =>
                {
                    contextOnRedirectToAccessDenied.Response.StatusCode = StatusCodes.Status403Forbidden;
                    contextOnRedirectToAccessDenied.Response.ContentType = "text/plain";
                    return contextOnRedirectToAccessDenied.Response.WriteAsync("Access denied. You do not have permission to access this resource.");
                },
                contextOnRedirectToLogin =>
                {
                    contextOnRedirectToLogin.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    contextOnRedirectToLogin.Response.ContentType = "text/plain";
                    return contextOnRedirectToLogin.Response.WriteAsync("Unauthorized access. Please login to continue.");
                },
                async contextOnValidatePrincipal =>
                {
                    var context = contextOnValidatePrincipal;
                    var userPrincipal = context.Principal;


                    // Retrieve user ID or other identifier
                    var userIdClaim = userPrincipal?.FindFirst(ClaimTypes.Name);


                    if (userIdClaim == null)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return;
                    }

                    var userId = userIdClaim.Value;

                    // Fetch updated user roles/claims from your data store
                    var userClaims = await GetUserClaimsAsync(userId);

                    // If claims have changed, reject the principal and sign the user back in with the updated claims
                    if (HaveClaimsChanged(userPrincipal.Claims, userClaims))
                    {
                        context.RejectPrincipal();

                        // Create a new ClaimsIdentity with updated claims
                        var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var newPrincipal = new ClaimsPrincipal(identity);

                        // Sign the user in with the new principal
                        await context.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);
                    }


                    //contextOnValidatePrincipal.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    //contextOnValidatePrincipal.Response.ContentType = "text/plain";
                    //return contextOnValidatePrincipal.Response.WriteAsync("Unauthorized access. Please login to continue.");
                }
             );
        }


        public static AuthenticationBuilder AddDefaultCookieConfig(this AuthenticationBuilder builder, Func<CookieSignedInContext, Task> OnSignedIn, Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToAccessDenied, Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToLogin, Func<CookieValidatePrincipalContext, Task> OnValidatePrincipal, string Name = "AuthorizationToken", TimeSpan? ExpireTimeSpan = null, bool SlidingExpiration = true)
        {
            ExpireTimeSpan ??= TimeSpan.FromDays(14);
            return builder.AddDefaultCookieConfig((cookie, events, options) =>
            {
                // Set the SameSite attribute of the cookie to Strict for enhanced security.
                cookie.SameSite = SameSiteMode.Strict;

                // Name of the cookie to be used for authentication.
                cookie.Name = Name;

                // Configure the cookie to be accessible only via the HTTP protocol.
                cookie.HttpOnly = true;

                // Ensure the cookie is always sent over HTTPS to protect against man-in-the-middle attacks.
                cookie.SecurePolicy = CookieSecurePolicy.Always;

                events.OnSignedIn = OnSignedIn;

                //events.OnValidatePrincipal = OnValidatePrincipal;

                // Event triggered when a user is authenticated but lacks the required permissions for a specific resource.
                // Instead of the default behavior of redirecting to an "Access Denied" page, it returns a 403 Forbidden status code.
                // This is typically used when the user is authenticated but is not authorized to perform a requested action.
                events.OnRedirectToAccessDenied = OnRedirectToAccessDenied;

                // Event triggered when authentication fails (e.g., no valid authentication cookie or session).
                // This handler prevents the default redirection to a login page, instead returning a 401 Unauthorized status code.
                // Useful for APIs or scenarios where redirection is not desired and the client expects status codes for handling.
                events.OnRedirectToLogin = OnRedirectToLogin;

                // Set the expiration time span of the cookie to 14 days.
                options.ExpireTimeSpan = ExpireTimeSpan.Value;

                // Enable sliding expiration to reset the cookie expiration time on each request during active sessions.
                options.SlidingExpiration = SlidingExpiration;
            });
        }

        public static AuthenticationBuilder AddDefaultCookieConfig(this AuthenticationBuilder builder, Action<CookieBuilder, CookieAuthenticationEvents, CookieAuthenticationOptions> action)
        {
            return builder.AddCookie(options =>
            {
                action.Invoke(options.Cookie, options.Events, options);
            });
        }
    }
}