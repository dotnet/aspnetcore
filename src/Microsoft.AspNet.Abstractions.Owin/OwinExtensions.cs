using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Owin;

namespace Owin
{
    public static class OwinExtensions
    {
        public static void RunHttpContext(this IAppBuilder app, Func<HttpContext, Task> handler)
        {
            app.Run(context =>
            {
                var httpContext = new OwinHttpContext(context);

                return handler(httpContext);
            });
        }
    }
}
