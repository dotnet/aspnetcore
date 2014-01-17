using System;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class ViewContext : RequestContext
    {
        public ViewContext(IOwinContext context, IRouteData routeData, object model) :
            base(context, routeData)
        {
            Model = model;
        }

        public IServiceProvider ServiceProvider { get; set; }

        public object Model { get; private set; }
    }
}
