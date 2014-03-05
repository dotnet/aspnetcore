using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing.Owin;
using Microsoft.AspNet.Routing.Template;

namespace MvcSample.Web
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
#if NET45
            var appRoot = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
#else 
            var appRoot = (string)typeof(AppDomain).GetRuntimeMethod("GetData", new[] { typeof(string) }).Invoke(AppDomain.CurrentDomain, new object[] { "APPBASE" });
#endif

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