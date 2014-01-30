using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class RequestContext
    {
        public RequestContext(HttpContext context, IRouteData routeData)
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

        public virtual HttpContext HttpContext { get; set; }
    }
}
