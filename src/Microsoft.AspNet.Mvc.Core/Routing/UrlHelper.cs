// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// An implementation of <see cref="IUrlHelper"/> that contains methods to
    /// build URLs for ASP.NET MVC within an application.
    /// </summary>
    public class UrlHelper : IUrlHelper
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlHelper"/> class using the specified action context and
        /// action selector.
        /// </summary>
        /// <param name="actionContext">
        /// The <see cref="Mvc.ActionContext"/> for the current request.
        /// </param>
        public UrlHelper(ActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }
            
            ActionContext = actionContext;
        }

        /// <inheritdoc />
        public ActionContext ActionContext { get; }

        protected RouteValueDictionary AmbientValues => ActionContext.RouteData.Values;

        protected HttpContext HttpContext => ActionContext.HttpContext;

        protected IRouter Router => ActionContext.RouteData.Routers[0];

        /// <inheritdoc />
        public virtual string Action(UrlActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var valuesDictionary = new RouteValueDictionary(actionContext.Values);

            if (actionContext.Action == null)
            {
                object action;
                if (!valuesDictionary.ContainsKey("action") &&
                    AmbientValues.TryGetValue("action", out action))
                {
                    valuesDictionary["action"] = action;
                }
            }
            else
            {
                valuesDictionary["action"] = actionContext.Action;
            }

            if (actionContext.Controller == null)
            {
                object controller;
                if (!valuesDictionary.ContainsKey("controller") &&
                    AmbientValues.TryGetValue("controller", out controller))
                {
                    valuesDictionary["controller"] = controller;
                }
            }
            else
            {
                valuesDictionary["controller"] = actionContext.Controller;
            }

            var path = GeneratePathFromRoute(valuesDictionary);
            if (path == null)
            {
                return null;
            }

            return GenerateUrl(actionContext.Protocol, actionContext.Host, path, actionContext.Fragment);
        }

        /// <inheritdoc />
        public virtual bool IsLocalUrl(string url)
        {
            return
                !string.IsNullOrEmpty(url) &&

                // Allows "/" or "/foo" but not "//" or "/\".
                ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) ||

                // Allows "~/" or "~/foo".
                (url.Length > 1 && url[0] == '~' && url[1] == '/'));
        }

        /// <inheritdoc />
        public virtual string RouteUrl(UrlRouteContext routeContext)
        {
            if (routeContext == null)
            {
                throw new ArgumentNullException(nameof(routeContext));
            }

            var valuesDictionary = new RouteValueDictionary(routeContext.Values);

            var path = GeneratePathFromRoute(routeContext.RouteName, valuesDictionary);
            if (path == null)
            {
                return null;
            }

            return GenerateUrl(routeContext.Protocol, routeContext.Host, path, routeContext.Fragment);
        }

        private string GeneratePathFromRoute(RouteValueDictionary values)
        {
            return GeneratePathFromRoute(routeName: null, values: values);
        }

        /// <summary>
        /// Generates the absolute path of the url for the specified route values by
        /// using the specified route name.
        /// </summary>
        /// <param name="routeName">The name of the route that is used to generate the URL.</param>
        /// <param name="values">A dictionary that contains the parameters for a route.</param>
        /// <returns>The absolute path of the URL.</returns>
        protected virtual string GeneratePathFromRoute(string routeName, RouteValueDictionary values)
        {
            var context = new VirtualPathContext(HttpContext, AmbientValues, values, routeName);
            var pathData = Router.GetVirtualPath(context);
            if (pathData == null)
            {
                return null;
            }

            // VirtualPathData.VirtualPath returns string.Empty for null.
            Debug.Assert(pathData.VirtualPath != null);

            var fullPath = HttpContext.Request.PathBase.Add(pathData.VirtualPath).Value;
            if (fullPath.Length == 0)
            {
                return "/";
            }
            else
            {
                return fullPath;
            }
        }

        /// <inheritdoc />
        public virtual string Content(string contentPath)
        {
            if (string.IsNullOrEmpty(contentPath))
            {
                return null;
            }
            else if (contentPath[0] == '~')
            {
                var segment = new PathString(contentPath.Substring(1));
                var applicationPath = HttpContext.Request.PathBase;

                return applicationPath.Add(segment).Value;
            }

            return contentPath;
        }

        /// <inheritdoc />
        public virtual string Link(string routeName, object values)
        {
            return RouteUrl(new UrlRouteContext()
            {
                RouteName = routeName,
                Values = values,
                Protocol = HttpContext.Request.Scheme,
                Host = HttpContext.Request.Host.ToUriComponent()
            });
        }

        private string GenerateUrl(string protocol, string host, string path, string fragment)
        {
            // We should have a robust and centrallized version of this code. See HttpAbstractions#28
            Debug.Assert(path != null);

            var url = path;
            if (!string.IsNullOrEmpty(fragment))
            {
                url += "#" + fragment;
            }

            if (string.IsNullOrEmpty(protocol) && string.IsNullOrEmpty(host))
            {
                // We're returning a partial url (just path + query + fragment), but we still want it
                // to be rooted.
                if (!url.StartsWith("/", StringComparison.Ordinal))
                {
                    url = "/" + url;
                }

                return url;
            }
            else
            {
                protocol = string.IsNullOrEmpty(protocol) ? "http" : protocol;
                host = string.IsNullOrEmpty(host) ? HttpContext.Request.Host.Value : host;

                url = protocol + "://" + host + url;
                return url;
            }
        }
    }
}