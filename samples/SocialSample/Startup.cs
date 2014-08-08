using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Security.Facebook;

namespace CookieSample
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            app.UseErrorPage();

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions()
            {
            });

            app.UseFacebookAuthentication(new FacebookAuthenticationOptions()
            {
                AppId = "569522623154478",
                AppSecret = "a124463c4719c94b4228d9a240e5dc1a",
            });

            app.Run(async context =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    context.Response.Challenge("Facebook");
                    return;
                }

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello " + context.User.Identity.Name);
            });
        }
    }
}