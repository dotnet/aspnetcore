using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Security.Cookies;

namespace CookieSample
{
    public class Startup
    {
        public void Configure(IBuilder app)
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