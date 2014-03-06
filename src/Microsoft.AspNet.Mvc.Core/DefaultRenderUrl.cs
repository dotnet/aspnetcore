using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultRenderUrl : IRenderUrl
    {
        private readonly HttpContext _httpContext;
        private readonly IRouter _router;
        private readonly IDictionary<string, object> _ambientValues;
 
        public DefaultRenderUrl(HttpContext httpContext, IRouter router, IDictionary<string, object> ambientValues)
        {
            _httpContext = httpContext;
            _router = router;
            _ambientValues = ambientValues;
        }

        public virtual string Action(string action, string controller, object values)
        {
            var valuesDictionary = new RouteValueDictionary(values);

            if (action != null)
            {
                valuesDictionary["action"] = action;
            }

            if (controller != null)
            {
                valuesDictionary["controller"] = controller;
            }

            return RouteCore(valuesDictionary);
        }

        public virtual string Route(object values)
        {
            return RouteCore(new RouteValueDictionary(values));

        }

        protected virtual string RouteCore(IDictionary<string, object> values)
        {
            var context = new VirtualPathContext(_httpContext, _ambientValues, values);
            var path = _router.GetVirtualPath(context);

            // We need to add the host part in here, currently blocked on http abstractions support. 
            // The intent is to use full URLs by default.
            return _httpContext.Request.PathBase + path;
        }
    }
}
