using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookieSessionSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Information);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                SessionStore = new MemoryCacheTicketStore()
            });

            app.Run(async context =>
            {
                if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // Make a large identity
                    var claims = new List<Claim>(1001);
                    claims.Add(new Claim(ClaimTypes.Name, "bob"));
                    for (int i = 0; i < 1000; i++)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "SomeRandomGroup" + i, ClaimValueTypes.String, "IssuedByBob", "OriginalIssuerJoe"));
                    }

                    await context.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello First timer");
                    return;
                }

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello old timer");
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseIISPlatformHandlerUrl()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}