// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An implementation of <see cref="IUrlHelper"/> that contains methods to
    /// build URLs for ASP.NET MVC within an application.
    /// </summary>
    public class UrlHelper : IUrlHelper
    {
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IActionSelector _actionSelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlHelper"/> class using the specified action context and
        /// action selector.
        /// </summary>
        /// <param name="actionContextAccessor">The <see cref="IActionContextAccessor"/> to access the action context
        /// of the current request.</param>
        /// <param name="actionSelector">The <see cref="IActionSelector"/> to be used for verifying the correctness of
        /// supplied parameters for a route.
        /// </param>
        public UrlHelper(IActionContextAccessor actionContextAccessor, IActionSelector actionSelector)
        {
            _actionContextAccessor = actionContextAccessor;
            _actionSelector = actionSelector;
        }

        protected IDictionary<string, object> AmbientValues => ActionContext.RouteData.Values;

        protected ActionContext ActionContext => _actionContextAccessor.ActionContext;

        protected HttpContext HttpContext => ActionContext.HttpContext;

        protected IRouter Router => ActionContext.RouteData.Routers[0];

        /// <inheritdoc />
        public virtual string Action(UrlActionContext actionContext)
        {
            var valuesDictionary = PropertyHelper.ObjectToDictionary(actionContext.Values);

            if (actionContext.Action != null)
            {
                valuesDictionary["action"] = actionContext.Action;
            }

            if (actionContext.Controller != null)
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
        public bool IsLocalUrl(string url)
        {
            return UrlUtility.IsLocalUrl(url);
        }

        /// <inheritdoc />
        public virtual string RouteUrl(UrlRouteContext routeContext)
        {
            var valuesDictionary = PropertyHelper.ObjectToDictionary(routeContext.Values);

            var path = GeneratePathFromRoute(routeContext.RouteName, valuesDictionary);
            if (path == null)
            {
                return null;
            }

            return GenerateUrl(routeContext.Protocol, routeContext.Host, path, routeContext.Fragment);
        }

        private string GeneratePathFromRoute(IDictionary<string, object> values)
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
        protected virtual string GeneratePathFromRoute(string routeName, IDictionary<string, object> values)
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