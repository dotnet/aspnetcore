using System;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;
using System.Net.Http.Formatting;

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

            // todo: inject
            InjectFormatters();
        }

        private void InjectFormatters()
        {
            Formatters = new MediaTypeFormatterCollection();
            Formatters.Add(new JsonMediaTypeFormatter());
            //Formatters.Add(new XmlMediaTypeFormatter());
            //Formatters.Add(new JQueryMvcFormUrlEncodedFormatter());
        }

        public virtual IRouteData RouteData { get; set; }

        public virtual IOwinContext HttpContext { get; set; }

        public virtual MediaTypeFormatterCollection Formatters { get; private set; }
    }
}
