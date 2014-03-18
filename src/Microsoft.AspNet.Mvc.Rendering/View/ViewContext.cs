using System;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewContext
    {
        public ViewContext(HttpContext context, ViewData viewData, IServiceProvider serviceProvider)
        {
            HttpContext = context;
            ViewData = viewData;
            ServiceProvider = serviceProvider;
        }

        public HttpContext HttpContext { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public IUrlHelper Url { get; set; }

        public ViewData ViewData { get; private set; }
    }
}
