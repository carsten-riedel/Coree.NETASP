using System.Net;
using System.Security.Claims;
using System.Xml.Linq;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Coree.NETASP.Services.CookieAuth
{
    public interface ICookieAuthHandler
    {
        Task<bool> AuthenticateAsync(string username, string password);

        Task<List<Claim>> GetClaimsAsync(string username);

        Task ValidatePrincipalAsync(CookieValidatePrincipalContext context);

        Task SignedInAsync(CookieSignedInContext context);

        Task RedirectToLogoutAsync(RedirectContext<CookieAuthenticationOptions> context);

        Task RedirectToAccessDeniedAsync(RedirectContext<CookieAuthenticationOptions> context);

        Task RedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> context);

        Task CheckSlidingExpirationAsync(CookieSlidingExpirationContext context);

        Task SigningInAsync(CookieSigningInContext context);

        Task SigningOutAsync(CookieSigningOutContext context);
    }

    public static class AuthExtensions
    {
        public static void AddCookieAuthHandler(this IServiceCollection services, string CookieName = "AuthorizationToken")
        {
            services.AddAuthentication(config => { config.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; }).AddDefault(CookieName);
        }

        private static AuthenticationBuilder AddDefault(this AuthenticationBuilder builder, string CookieName)
        {
            return builder.AddCookie(config =>
            {
                // Set the SameSite attribute of the cookie to Strict for enhanced security.
                config.Cookie.SameSite = SameSiteMode.Strict;

                // Name of the cookie to be used for authentication.
                config.Cookie.Name = CookieName;

                // Configure the cookie to be accessible only via the HTTP protocol.
                config.Cookie.HttpOnly = true;

                // Ensure the cookie is always sent over HTTPS to protect against man-in-the-middle attacks.
                config.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                config.Events.OnSignedIn = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.SignedInAsync(context);
                };

                config.Events.OnRedirectToLogout = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.RedirectToLogoutAsync(context);
                };

                config.Events.OnRedirectToAccessDenied = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.RedirectToAccessDeniedAsync(context);
                };

                config.Events.OnRedirectToLogin = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.RedirectToLoginAsync(context);
                };

                config.Events.OnValidatePrincipal = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.ValidatePrincipalAsync(context);
                };

                config.Events.OnCheckSlidingExpiration = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.CheckSlidingExpirationAsync(context);
                };

                config.Events.OnSigningIn = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.SigningInAsync(context);
                };

                config.Events.OnSigningOut = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<ICookieAuthHandler>();
                    await handler.SigningOutAsync(context);
                };
            });
        }
    }

    public class CookieAuthHandler : ICookieAuthHandler
    {
        public CookieAuthHandler()
        {
        }

        public Task<bool> AuthenticateAsync(string username, string password)
        {
            if (username == "pass" && password == "word")
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<List<Claim>> GetClaimsAsync(string username)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(ClaimTypes.GivenName, username),
                    new Claim(ClaimTypes.Email, username),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("Department", "Human Resources"),
                    new Claim("MyRoles", "fool")
                };
            return Task.FromResult(claims);
        }

        public Task CheckSlidingExpirationAsync(CookieSlidingExpirationContext context)
        {
            return Task.CompletedTask;
        }

        public Task RedirectToAccessDeniedAsync(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain";
            return context.Response.WriteAsync("Access denied. You do not have permission to access this resource.");
        }

        public Task RedirectToLoginAsync(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "text/plain";
            return context.Response.WriteAsync("Unauthorized access. Please login to continue.");
        }

        public Task RedirectToLogoutAsync(RedirectContext<CookieAuthenticationOptions> context)
        {
            throw new NotImplementedException();
        }

        public Task SignedInAsync(CookieSignedInContext context)
        {
            return Task.CompletedTask;
        }

        public Task SigningInAsync(CookieSigningInContext context)
        {
            return Task.CompletedTask;
        }

        public Task SigningOutAsync(CookieSigningOutContext context)
        {
            return Task.CompletedTask;
        }

        public Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            return Task.CompletedTask;
        }
    }
}