#if NET45
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
using Owin;

namespace MvcSample
{
    public class Startup
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationEnvironment _env;

        public Startup(IServiceProvider serviceProvider,
                       IApplicationEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _env = env;
        }

        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigureMvc);
        }

        private void ConfigureMvc(IBuilder builder)
        {
            var configuration = new Configuration();
            var services = MvcServices.GetDefaultServices(configuration, _env);
            var serviceProvider = new ServiceProvider().Add(services);

            var router = builder.UseRouter();

            var endpoint = ActivatorUtilities.CreateInstance<RouteEndpoint>(serviceProvider);

            router.Routes.Add(new TemplateRoute(
                endpoint,
                "{controller}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" } }));

            router.Routes.Add(new TemplateRoute(
                endpoint,
                "{controller}/{action}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" }, { "action", "Index" } }));
        }
    }
}
#endif