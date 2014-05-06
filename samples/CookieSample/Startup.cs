using System;
using System.Security.Claims;
using Microsoft.AspNet;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;

namespace CookieSample
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseServices(services =>
            {
                // TODO: Move to host.
                services.AddInstance<ILoggerFactory>(new NullLoggerFactory());
            });

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

        // TODO: Temp workaround until the host reliably provides logging.
        // If ILoggerFactory is never guaranteed, move this fallback into Microsoft.Framework.Logging.
        private class NullLoggerFactory : ILoggerFactory
        {
            public ILogger Create(string name)
            {
                return new NullLongger();
            }
        }

        private class NullLongger : ILogger
        {
            public bool WriteCore(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                return false;
            }
        }
    }
}