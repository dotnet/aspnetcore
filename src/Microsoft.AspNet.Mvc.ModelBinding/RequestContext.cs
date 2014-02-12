using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class RequestContext
    {
        public RequestContext(HttpContext context, IDictionary<string, object> routeValues)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (routeValues == null)
            {
                throw new ArgumentNullException("routeValues");
            }

            HttpContext = context;
            RouteValues = routeValues;
        }

        public virtual IDictionary<string, object> RouteValues { get; set; }

        public virtual HttpContext HttpContext { get; set; }
    }
}
