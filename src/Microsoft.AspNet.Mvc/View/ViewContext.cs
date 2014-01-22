using System;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class ViewContext : RequestContext
    {
        public ViewContext(IOwinContext context, IRouteData routeData, ViewData viewData) :
            base(context, routeData)
        {
            ViewData = viewData;
        }

        public IServiceProvider ServiceProvider { get; set; }

        public ViewData ViewData { get; private set; }
    }
}
