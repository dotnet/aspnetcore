
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
            string appRoot = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            var mvcServices = new MvcServices(appRoot);

            var router = builder.UseRouter();

            var endpoint = ActivatorUtilities.CreateInstance<RouteEndpoint>(mvcServices.Services);

            router.Add(new TemplateRoute(
                endpoint,
                "{controller}/{action}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" }, { "action", "Index" } }));
            router.Add(new TemplateRoute(
                endpoint,
                "{controller}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" } }));
        }
    }
}
#endif