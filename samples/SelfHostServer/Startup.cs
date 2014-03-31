using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Server.WebListener;

namespace SelfHostServer
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            var info = (ServerInformation)app.Server;
            info.Listener.AuthenticationManager.AuthenticationTypes = AuthenticationType.None;

            app.Run(async context =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello world");
            });
        }
    }
}
