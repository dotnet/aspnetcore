using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
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

        public string Action(string action, string controller, object values, string protocol, string host, string fragment)
        {
            var valuesDictionary = values as IDictionary<string, object>;
            if (valuesDictionary == null)
            {
                valuesDictionary = new RouteValueDictionary(values);
            }
            else
            {
                valuesDictionary = new RouteValueDictionary(valuesDictionary);
            }

            if (action != null)
            {
                valuesDictionary["action"] = action;
            }

            if (controller != null)
            {
                valuesDictionary["controller"] = controller;
            }

            var path = RouteCore(valuesDictionary);
            if (path == null)
            {
                return null;
            }

            return GenerateUrl(protocol, host, path, fragment);
        }

        public string RouteUrl(object values, string protocol, string host, string fragment)
        {
            var path = RouteCore(new RouteValueDictionary(values));
            if (path == null)
            {
                return null;
            }

            return GenerateUrl(protocol, host, path, fragment);
        }

        private string RouteCore(IDictionary<string, object> values)
        {
            var context = new VirtualPathContext(_httpContext, _ambientValues, values);

            var path = _router.GetVirtualPath(context);

            // See Routing Issue#31
            PathString pathString;
            if (path.Length > 0 && !path.StartsWith("/", StringComparison.Ordinal))
            {
                pathString = new PathString("/" + path);
            }
            else
            {
                pathString = new PathString(path);
            }

            return _httpContext.Request.PathBase.Add(pathString).Value;
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

        private string GenerateUrl(string protocol, string host, string path, string fragment)
        {
            Contract.Assert(path != null);

            var url = path;
            if (!string.IsNullOrEmpty(fragment))
            {
                url = url + "#" + fragment;
            }

            if (!string.IsNullOrEmpty(protocol) || !string.IsNullOrEmpty(host))
            {
                protocol = string.IsNullOrEmpty(protocol) ? "http" : protocol;
                host = string.IsNullOrEmpty(host) ? _httpContext.Request.Host.Value : host;

                url = protocol + "://" + host + url;
            }

            return url;
        }
    }
}
