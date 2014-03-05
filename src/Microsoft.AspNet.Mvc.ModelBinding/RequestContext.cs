using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class RequestContext
    {
        public RequestContext([NotNull]HttpContext context,
                              [NotNull]IDictionary<string, object> routeValues)
        {
            HttpContext = context;
            RouteValues = routeValues;
        }

        public virtual IDictionary<string, object> RouteValues { get; set; }

        public virtual HttpContext HttpContext { get; set; }
    }
}
