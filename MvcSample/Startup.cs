using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MvcSample.Startup))]

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            var handler = new MvcHandler();

            // Pretending to be routing
            app.Run(async context =>
            {
                await handler.ExecuteAsync(context);
            });
        }
    }
}
