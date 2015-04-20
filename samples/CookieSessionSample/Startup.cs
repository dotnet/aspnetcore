using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace CookieSessionSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebEncoders();
            services.AddDataProtection();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Information);

            app.UseCookieAuthentication(options =>
            {
                options.AutomaticAuthentication = true;
                options.SessionStore = new MemoryCacheSessionStore();
            });

            app.Run(async context =>
            {
                if (string.IsNullOrEmpty(context.User.Identity.Name))
                {
                    // Make a large identity
                    var claims = new List<Claim>(1001);
                    claims.Add(new Claim(ClaimTypes.Name, "bob"));
                    for (int i = 0; i < 1000; i++)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "SomeRandomGroup" + i, ClaimValueTypes.String, "IssuedByBob", "OriginalIssuerJoe"));
                    }
                    context.Response.SignIn(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims)));
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello First timer");
                    return;
                }

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello old timer");
            });
        }
    }
}