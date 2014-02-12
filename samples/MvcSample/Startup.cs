
#if NET45
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing.Owin;
using Microsoft.AspNet.Routing.Template;
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

            var router = builder.UseRouter();

            var endpoint = ActivatorUtilities.CreateInstance<RouteEndpoint>(mvcServices.Services);
            router.Add(new TemplateRoute(
                endpoint, 
                "{controller}/{action}", 
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase){ { "controller", "Home"}, { "action", "Index" } }));
        }
    }
}
#endif