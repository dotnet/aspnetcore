using System;
using System.Security.Claims;
using Microsoft.AspNet;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace CookieSample
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {

            });

            app.Run(async context =>
            {
                if (context.User == null || !context.User.Identity.IsAuthenticated)
                {
                    context.Response.SignIn(new ClaimsIdentity(new[] { new Claim("name", "bob") }, CookieAuthenticationDefaults.AuthenticationType));                    

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