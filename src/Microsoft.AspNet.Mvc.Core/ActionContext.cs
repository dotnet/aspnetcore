using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class ActionContext
    {
        public ActionContext(HttpContext httpContext, IDictionary<string, object> routeValues, ActionDescriptor actionDescriptor)
        {
            HttpContext = httpContext;
            RouteValues = routeValues;
            ActionDescriptor = actionDescriptor;
        }

        public HttpContext HttpContext { get; private set; }

        public IDictionary<string, object> RouteValues { get; private set; }

        public ActionDescriptor ActionDescriptor { get; private set; }
    }
}
