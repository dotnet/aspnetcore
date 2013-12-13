using System;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class RequestContext
    {
        public RequestContext(IOwinContext context, IRouteData routeData)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (routeData == null)
            {
                throw new ArgumentNullException("routeData");
            }

            HttpContext = context;
            RouteData = routeData;
        }

        public virtual IRouteData RouteData { get; set; }

        public virtual IOwinContext HttpContext { get; set; }
    }
}
