// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Razor page specific extensions for <see cref="IUrlHelper"/>.
    /// </summary>
    public static class PageUrlHelperExtensions
    {
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
        /// <param name="values">An object that contains route values.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            object values)
            => Page(urlHelper, pageName, values, protocol: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            object values,
            string protocol)
            => Page(urlHelper, pageName, values, protocol, host: null, fragment: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
            object values,
            string protocol,
            string host)
            => Page(urlHelper, pageName, values, protocol, host, fragment: null);

        /// <summary>
        /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
        /// <param name="pageName">The page name to generate the url for.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
        /// <param name="host">The host name for the URL.</param>
        /// <param name="fragment">The fragment for the URL.</param>
        /// <returns>The generated URL.</returns>
        public static string Page(
            this IUrlHelper urlHelper,
            string pageName,
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
            if (pageName == null)
            {
                if (!routeValues.ContainsKey("page") &&
                    ambientValues.TryGetValue("page", out var value))
                {
                    routeValues["page"] = value;
                }
            }
            else
            {
                routeValues["page"] = pageName;
            }

            if (!routeValues.ContainsKey("formaction") && 
                ambientValues.TryGetValue("formaction", out var formaction))
            {
                // Clear out formaction unless it's explicitly specified in the routeValues.
                routeValues["formaction"] = null;
            }

            return urlHelper.RouteUrl(
                routeName: null,
                values: routeValues,
                protocol: protocol,
                host: host,
                fragment: fragment);
        }
    }
}
