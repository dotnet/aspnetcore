using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace OpenIdConnectSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebEncoders();
            services.AddDataProtection();
            services.Configure<ExternalAuthenticationOptions>(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Information);

            app.UseCookieAuthentication(options =>
            {
                options.AutomaticAuthentication = true;
            });

            app.UseOpenIdConnectAuthentication(options =>
                {
                    options.ClientId = "fe78e0b4-6fe7-47e6-812c-fb75cee266a4";
                    options.Authority = "https://login.windows.net/cyrano.onmicrosoft.com";
                    options.RedirectUri = "http://localhost:42023";
                });

            app.Run(async context =>
            {
                if (string.IsNullOrEmpty(context.User.Identity.Name))
                {
                    context.Response.Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationScheme);

                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Hello First timer");
                    return;
                }

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello Authenticated User");
            });


        }
    }
}
