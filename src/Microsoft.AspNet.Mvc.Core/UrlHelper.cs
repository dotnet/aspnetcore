using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class UrlHelper : IUrlHelper
    {
        private readonly HttpContext _httpContext;
        private readonly IRouter _router;
        private readonly IDictionary<string, object> _ambientValues;

        public UrlHelper(IContextAccessor<ActionContext> contextAccessor)
        {
            _httpContext = contextAccessor.Value.HttpContext;
            _router = contextAccessor.Value.Router;
            _ambientValues = contextAccessor.Value.RouteValues;
        }

        public string Action(string action, string controller, object values)
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

        public string Route(object values)
        {
            return RouteCore(new RouteValueDictionary(values));
        }

        private string RouteCore(IDictionary<string, object> values)
        {
            var context = new VirtualPathContext(_httpContext, _ambientValues, values);
            var path = _router.GetVirtualPath(context);

            // We need to add the host part in here, currently blocked on http abstractions support. 
            // The intent is to use full URLs by default.
            return _httpContext.Request.PathBase + path;
        }

        public string Content([NotNull] string contentPath)
        {
            return GenerateClientUrl(_httpContext.Request.PathBase, contentPath);
        }

        private static string GenerateClientUrl([NotNull] PathString applicationPath, 
                                                [NotNull] string path)
        {
            if (path.StartsWith("~/", StringComparison.Ordinal))
            {
                var segment = new PathString(path.Substring(1));
                return applicationPath.Add(segment).Value;
            }
            return path;
        } 
    }
}
