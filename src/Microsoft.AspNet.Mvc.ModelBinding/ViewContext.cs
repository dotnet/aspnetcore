using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ViewContext : RequestContext
    {
        public ViewContext(HttpContext context, IDictionary<string, object> routeValues, ViewData viewData) :
            base(context, routeValues)
        {
            ViewData = viewData;
        }

        public IServiceProvider ServiceProvider { get; set; }

        public ViewData ViewData { get; private set; }
    }
}
