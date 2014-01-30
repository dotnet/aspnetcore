using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ViewContext : RequestContext
    {
        public ViewContext(HttpContext context, IRouteData routeData, ViewData viewData) :
            base(context, routeData)
        {
            ViewData = viewData;
        }

        public IServiceProvider ServiceProvider { get; set; }

        public ViewData ViewData { get; private set; }
    }
}
