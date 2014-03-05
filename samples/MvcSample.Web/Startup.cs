using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing.Owin;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Net.Runtime;

namespace MvcSample.Web
{
    public class Startup
    {
        private readonly IApplicationEnvironment _env;

        public Startup(IApplicationEnvironment env)
        {
            _env = env;
        }

        public void Configuration(IBuilder app)
        {
            var configuration = new Configuration();
            var serviceProvider = new ServiceProvider().Add(MvcServices.GetDefaultServices(configuration, _env));

            var router = app.UseRouter();

            var endpoint = ActivatorUtilities.CreateInstance<RouteEndpoint>(serviceProvider);

            router.Routes.Add(new TemplateRoute(
                endpoint,
                "{controller}/{action}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" }, { "action", "Index" } }));
        }
    }
}