using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.Framework.DependencyInjection;

namespace CookieSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebEncoders();
            services.AddDataProtection();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCookieAuthentication(options =>
            {
                options.AutomaticAuthentication = true;
            });

            app.Run(async context =>
            {
                if (string.IsNullOrEmpty(context.User.Identity.Name))
                {
                    context.Response.SignIn(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "bob") })));
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