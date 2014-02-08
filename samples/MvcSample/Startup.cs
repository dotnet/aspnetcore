#if NET45
using System;
using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.Startup;
using Owin;

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigureMvc);
        }

        private void ConfigureMvc(IBuilder builder)
        {
            // HACK appbase doesn't seem to work. When in VS we're pointing at bin\Debug\Net45, so move up 3 directories
            string appRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."));

            var mvcServices = new MvcServices(appRoot);

            mvcServices.Finalize();

            var handler = (MvcHandler)(ActivatorUtilities.CreateInstance(mvcServices.Services, typeof(MvcHandler)));

            builder.Run(async context =>
            {
                // Pretending to be routing
                var routeData = new FakeRouteData(context);

                await handler.ExecuteAsync(context, routeData);
            });
        }
    }
}
#endif