
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
using Owin;

namespace MvcSample
{
    public class Startup
    {
        private IServiceProvider _serviceProvider;

        public Startup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigureMvc);
        }

        private void ConfigureMvc(IBuilder builder)
        {
            string appRoot = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            var mvcServices = new MvcServices(appRoot, _serviceProvider);

            var router = builder.UseRouter();

            var endpoint = ActivatorUtilities.CreateInstance<RouteEndpoint>(mvcServices.Services);

            router.Add(new TemplateRoute(
                endpoint,
                "{controller}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" } }));

            router.Add(new TemplateRoute(
                endpoint,
                "{controller}/{action}",
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "controller", "Home" }, { "action", "Index" } }));
            
        }
    }
}
#endif