using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.Security.OpenIdConnect;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.Security;

namespace OpenIdConnectSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseServices(services =>
            {
                services.AddDataProtection();
                services.Configure<ExternalAuthenticationOptions>(options =>
                {
                    options.SignInAsAuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType;
                });

            });

            app.UseCookieAuthentication(options =>
            {
                options.AuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType;
            });

            app.UseOpenIdConnectAuthentication(options =>
                {
                    options.ClientId = "fe78e0b4-6fe7-47e6-812c-fb75cee266a4";
                    options.Authority = "https://login.windows.net/cyrano.onmicrosoft.com";
                    options.RedirectUri = "http://localhost:42023";
                    options.SignInAsAuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType;
                    options.AuthenticationType = OpenIdConnectAuthenticationDefaults.AuthenticationType;
                });

            app.Run(async context =>
            {
                if (context.User == null || !context.User.Identity.IsAuthenticated)
                {
                    context.Response.Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);

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
