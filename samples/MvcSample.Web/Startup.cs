using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.Startup;
using Microsoft.AspNet.Routing.Owin;
using Microsoft.AspNet.Routing.Template;

namespace MvcSample.Web
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            string appRoot = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            var mvcServices = new MvcServices(appRoot);

            var router = app.UseRouter();

            var endpoint = ActivatorUtilities.CreateInstance<RouteEndpoint>(mvcServices.Services);

            router.Add(new TemplateRoute(
                endpoint,
                "{controller}/{action}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" }, { "action", "Index" } }));
        }
    }
}