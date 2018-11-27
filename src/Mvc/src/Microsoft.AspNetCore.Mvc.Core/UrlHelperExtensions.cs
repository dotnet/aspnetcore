// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Generates a URL with an absolute path for an action method.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(this IUrlHelper helper)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(
                action: null,
                controller: null,
                values: null,
                protocol: null,
                host: null,
                fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> name.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(this IUrlHelper helper, string action)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller: null, values: null, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> name and route <paramref name="values"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(this IUrlHelper helper, string action, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller: null, values: values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> and <paramref name="controller"/> names.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(this IUrlHelper helper, string action, string controller)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values: null, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> name, <paramref name="controller"/> name, and route <paramref name="values"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(this IUrlHelper helper, string action, string controller, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>, and
        /// <paramref name="protocol"/> to use.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <returns>The generated URL.</returns>
        public static string Action(
            this IUrlHelper helper,
            string action,
            string controller,
            object values,
            string protocol)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values, protocol, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>,
        /// <paramref name="protocol"/> to use, and <paramref name="host"/> name.
        /// Generates an absolute URL if the <paramref name="protocol"/> and <paramref name="host"/> are
        /// non-<c>null</c>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(
            this IUrlHelper helper,
            string action,
            string controller,
            object values,
            string protocol,
            string host)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(action, controller, values, protocol, host, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for an action method, which contains the specified
        /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>,
        /// <paramref name="protocol"/> to use, <paramref name="host"/> name, and <paramref name="fragment"/>.
        /// Generates an absolute URL if the <paramref name="protocol"/> and <paramref name="host"/> are
        /// non-<c>null</c>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string Action(
            this IUrlHelper helper,
            string action,
            string controller,
            object values,
            string protocol,
            string host,
            string fragment)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.Action(new UrlActionContext()
            {
                Action = action,
                Controller = controller,
                Host = host,
                Values = values,
                Protocol = protocol,
                Fragment = fragment
            });
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified route <paramref name="values"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string RouteUrl(this IUrlHelper helper, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName: null, values: values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="routeName"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <returns>The generated URL.</returns>
        public static string RouteUrl(this IUrlHelper helper, string routeName)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values: null, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="routeName"/> and route
        /// <paramref name="values"/>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string RouteUrl(this IUrlHelper helper, string routeName, object values)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values, protocol: null, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified route <paramref name="routeName"/> and route
        /// <paramref name="values"/>, which contains the specified <paramref name="protocol"/> to use.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <returns>The generated URL.</returns>
        public static string RouteUrl(
            this IUrlHelper helper,
            string routeName,
            object values,
            string protocol)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values, protocol, host: null, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified route <paramref name="routeName"/> and route
        /// <paramref name="values"/>, which contains the specified <paramref name="protocol"/> to use and
        /// <paramref name="host"/> name. Generates an absolute URL if
        /// <see cref="UrlActionContext.Protocol"/> and <see cref="UrlActionContext.Host"/> are non-<c>null</c>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string RouteUrl(
            this IUrlHelper helper,
            string routeName,
            object values,
            string protocol,
            string host)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(routeName, values, protocol, host, fragment: null);
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified route <paramref name="routeName"/> and route
        /// <paramref name="values"/>, which contains the specified <paramref name="protocol"/> to use,
        /// <paramref name="host"/> name and <paramref name="fragment"/>. Generates an absolute URL if
        /// <see cref="UrlActionContext.Protocol"/> and <see cref="UrlActionContext.Host"/> are non-<c>null</c>.
        /// </summary>
        /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="routeName">The name of the route that is used to generate URL.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string RouteUrl(
            this IUrlHelper helper,
            string routeName,
            object values,
            string protocol,
            string host,
            string fragment)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            return helper.RouteUrl(new UrlRouteContext()
            {
                RouteName = routeName,
                Values = values,
                Protocol = protocol,
                Host = host,
                Fragment = fragment
            });
        }

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(this IUrlHelper urlHelper, string pageName)
            => Page(urlHelper, pageName, values: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="pageHandler">The handler to generate the url for.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(this IUrlHelper urlHelper, string pageName, string pageHandler)
            => Page(urlHelper, pageName, pageHandler, values: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(this IUrlHelper urlHelper, string pageName, object values)
            => Page(urlHelper, pageName, pageHandler: null, values: values);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="pageHandler">The handler to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            string pageHandler,
            object values)
            => Page(urlHelper, pageName, pageHandler, values, protocol: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="pageHandler">The handler to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            string pageHandler,
            object values,
            string protocol)
            => Page(urlHelper, pageName, pageHandler, values, protocol, host: null, fragment: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="pageHandler">The handler to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            string pageHandler,
            object values,
            string protocol,
            string host)
            => Page(urlHelper, pageName, pageHandler, values, protocol, host, fragment: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="pageHandler">The handler to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            string pageHandler,
            object values,
            string protocol,
            string host,
            string fragment)
        {
            if (urlHelper == null)
            {
                throw new ArgumentNullException(nameof(urlHelper));
            }

            var routeValues = new RouteValueDictionary(values);
            var ambientValues = urlHelper.ActionContext.RouteData.Values;
            if (string.IsNullOrEmpty(pageName))
            {
                if (!routeValues.ContainsKey("page") &&
                    ambientValues.TryGetValue("page", out var value))
                {
                    routeValues["page"] = value;
                }
            }
            else
            {
                routeValues["page"] = CalculatePageName(urlHelper.ActionContext, pageName);
            }

            if (string.IsNullOrEmpty(pageHandler))
            {
                if (!routeValues.ContainsKey("handler") &&
                    ambientValues.TryGetValue("handler", out var handler))
                {
                    // Clear out formaction unless it's explicitly specified in the routeValues.
                    routeValues["handler"] = null;
                }
            }
            else
            {
                routeValues["handler"] = pageHandler;
            }

            return urlHelper.RouteUrl(
                routeName: null,
                values: routeValues,
                protocol: protocol,
                host: host,
                fragment: fragment);
        }

        private static object CalculatePageName(ActionContext actionContext, string pageName)
        {
            Debug.Assert(pageName.Length > 0);
            // Paths not qualified with a leading slash are treated as relative to the current page.
            if (pageName[0] != '/')
            {
                var currentPagePath = NormalizedRouteValue.GetNormalizedRouteValue(actionContext, "page");
                if (string.IsNullOrEmpty(currentPagePath))
                {
                    // Disallow the use sibling page routing, a Razor page specific feature, from a non-page action.
                    throw new InvalidOperationException(Resources.FormatUrlHelper_RelativePagePathIsNotSupported(pageName));
                }

                return ViewEnginePath.CombinePath(currentPagePath, pageName);
            }

            return pageName;
        }
    }
}
