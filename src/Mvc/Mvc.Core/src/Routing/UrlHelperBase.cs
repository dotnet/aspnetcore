// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public abstract class UrlHelperBase : IUrlHelper
    {
        // Perf: Share the StringBuilder object across multiple calls of GenerateURL for this UrlHelper
        private StringBuilder _stringBuilder;

        // Perf: Reuse the RouteValueDictionary across multiple calls of Action for this UrlHelper
        private readonly RouteValueDictionary _routeValueDictionary;

        protected UrlHelperBase(ActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            ActionContext = actionContext;
            AmbientValues = actionContext.RouteData.Values;
            _routeValueDictionary = new RouteValueDictionary();
        }

        /// <summary>
        /// Gets the <see cref="RouteValueDictionary"/> associated with the current request.
        /// </summary>
        protected RouteValueDictionary AmbientValues { get; }

        /// <inheritdoc />
        public ActionContext ActionContext { get; }

        /// <inheritdoc />
        public virtual bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            // Allows "/" or "/foo" but not "//" or "/\".
            if (url[0] == '/')
            {
                // url is exactly "/"
                if (url.Length == 1)
                {
                    return true;
                }

                // url doesn't start with "//" or "/\"
                if (url[1] != '/' && url[1] != '\\')
                {
                    return true;
                }

                return false;
            }

            // Allows "~/" or "~/foo" but not "~//" or "~/\".
            if (url[0] == '~' && url.Length > 1 && url[1] == '/')
            {
                // url is exactly "~/"
                if (url.Length == 2)
                {
                    return true;
                }

                // url doesn't start with "~//" or "~/\"
                if (url[2] != '/' && url[2] != '\\')
                {
                    return true;
                }

                return false;
            }

            return false;
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
                var applicationPath = ActionContext.HttpContext.Request.PathBase;

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
                Protocol = ActionContext.HttpContext.Request.Scheme,
                Host = ActionContext.HttpContext.Request.Host.ToUriComponent()
            });
        }

        /// <inheritdoc />
        public abstract string Action(UrlActionContext actionContext);

        /// <inheritdoc />
        public abstract string RouteUrl(UrlRouteContext routeContext);

        protected RouteValueDictionary GetValuesDictionary(object values)
        {
            // Perf: RouteValueDictionary can be cast to IDictionary<string, object>, but it is
            // special cased to avoid allocating boxed Enumerator.
            if (values is RouteValueDictionary routeValuesDictionary)
            {
                _routeValueDictionary.Clear();
                foreach (var kvp in routeValuesDictionary)
                {
                    _routeValueDictionary.Add(kvp.Key, kvp.Value);
                }

                return _routeValueDictionary;
            }

            if (values is IDictionary<string, object> dictionaryValues)
            {
                _routeValueDictionary.Clear();
                foreach (var kvp in dictionaryValues)
                {
                    _routeValueDictionary.Add(kvp.Key, kvp.Value);
                }

                return _routeValueDictionary;
            }

            return new RouteValueDictionary(values);
        }

        protected string GenerateUrl(string protocol, string host, string virtualPath, string fragment)
        {
            if (virtualPath == null)
            {
                return null;
            }

            // Perf: In most of the common cases, GenerateUrl is called with a null protocol, host and fragment.
            // In such cases, we might not need to build any URL as the url generated is mostly same as the virtual path available in pathData.
            // For such common cases, this FastGenerateUrl method saves a string allocation per GenerateUrl call.
            if (TryFastGenerateUrl(protocol, host, virtualPath, fragment, out var url))
            {
                return url;
            }

            var builder = GetStringBuilder();
            try
            {
                var pathBase = ActionContext.HttpContext.Request.PathBase;

                if (string.IsNullOrEmpty(protocol) && string.IsNullOrEmpty(host))
                {
                    AppendPathAndFragment(builder, pathBase, virtualPath, fragment);
                    // We're returning a partial URL (just path + query + fragment), but we still want it to be rooted.
                    if (builder.Length == 0 || builder[0] != '/')
                    {
                        builder.Insert(0, '/');
                    }
                }
                else
                {
                    protocol = string.IsNullOrEmpty(protocol) ? "http" : protocol;
                    builder.Append(protocol);

                    builder.Append("://");

                    host = string.IsNullOrEmpty(host) ? ActionContext.HttpContext.Request.Host.Value : host;
                    builder.Append(host);
                    AppendPathAndFragment(builder, pathBase, virtualPath, fragment);
                }

                var path = builder.ToString();
                return path;
            }
            finally
            {
                // Clear the StringBuilder so that it can reused for the next call.
                builder.Clear();
            }
        }

        /// <summary>
        /// Generates a URI from the provided components.
        /// </summary>
        /// <param name="protocol">The URI scheme/protocol.</param>
        /// <param name="host">The URI host.</param>
        /// <param name="path">The URI path and remaining portions (path, query, and fragment).</param>
        /// <returns>
        /// An absolute URI if the <paramref name="protocol"/> or <paramref name="host"/> is specified, otherwise generates a
        /// URI with an absolute path.
        /// </returns>
        protected string GenerateUrl(string protocol, string host, string path)
        {
            // This method is similar to GenerateUrl, but it's used for EndpointRouting. It ignores pathbase and fragment
            // because those have already been incorporated.
            if (path == null)
            {
                return null;
            }

            // Perf: In most of the common cases, GenerateUrl is called with a null protocol, host and fragment.
            // In such cases, we might not need to build any URL as the url generated is mostly same as the virtual path available in pathData.
            // For such common cases, this FastGenerateUrl method saves a string allocation per GenerateUrl call.
            if (TryFastGenerateUrl(protocol, host, path, fragment: null, out var url))
            {
                return url;
            }

            var builder = GetStringBuilder();
            try
            {
                if (string.IsNullOrEmpty(protocol) && string.IsNullOrEmpty(host))
                {
                    AppendPathAndFragment(builder, pathBase: null, path, fragment: null);

                    // We're returning a partial URL (just path + query + fragment), but we still want it to be rooted.
                    if (builder.Length == 0 || builder[0] != '/')
                    {
                        builder.Insert(0, '/');
                    }
                }
                else
                {
                    protocol = string.IsNullOrEmpty(protocol) ? "http" : protocol;
                    builder.Append(protocol);

                    builder.Append("://");

                    host = string.IsNullOrEmpty(host) ? ActionContext.HttpContext.Request.Host.Value : host;
                    builder.Append(host);
                    AppendPathAndFragment(builder, pathBase: null, path, fragment: null);
                }

                return builder.ToString();
            }
            finally
            {
                // Clear the StringBuilder so that it can reused for the next call.
                builder.Clear();
            }
        }

        internal static void NormalizeRouteValuesForAction(
            string action,
            string controller,
            RouteValueDictionary values,
            RouteValueDictionary ambientValues)
        {
            object obj = null;
            if (action == null)
            {
                if (!values.ContainsKey("action") &&
                    (ambientValues?.TryGetValue("action", out obj) ?? false))
                {
                    values["action"] = obj;
                }
            }
            else
            {
                values["action"] = action;
            }

            if (controller == null)
            {
                if (!values.ContainsKey("controller") &&
                    (ambientValues?.TryGetValue("controller", out obj) ?? false))
                {
                    values["controller"] = obj;
                }
            }
            else
            {
                values["controller"] = controller;
            }
        }

        internal static void NormalizeRouteValuesForPage(
            ActionContext context,
            string page,
            string handler,
            RouteValueDictionary values,
            RouteValueDictionary ambientValues)
        {
            object value = null;
            if (string.IsNullOrEmpty(page))
            {
                if (!values.ContainsKey("page") &&
                    (ambientValues?.TryGetValue("page", out value) ?? false))
                {
                    values["page"] = value;
                }
            }
            else
            {
                values["page"] = CalculatePageName(context, ambientValues, page);
            }

            if (string.IsNullOrEmpty(handler))
            {
                if (!values.ContainsKey("handler") &&
                    (ambientValues?.ContainsKey("handler") ?? false))
                {
                    // Clear out form action unless it's explicitly specified in the routeValues.
                    values["handler"] = null;
                }
            }
            else
            {
                values["handler"] = handler;
            }
        }

        private static object CalculatePageName(ActionContext context, RouteValueDictionary ambientValues, string pageName)
        {
            Debug.Assert(pageName.Length > 0);
            // Paths not qualified with a leading slash are treated as relative to the current page.
            if (pageName[0] != '/')
            {
                // OK now we should get the best 'normalized' version of the page route value that we can.
                string currentPagePath;
                if (context != null)
                {
                    currentPagePath = NormalizedRouteValue.GetNormalizedRouteValue(context, "page");
                }
                else if (ambientValues != null)
                {
                    currentPagePath = Convert.ToString(ambientValues["page"], CultureInfo.InvariantCulture);
                }
                else
                {
                    currentPagePath = null;
                }

                if (string.IsNullOrEmpty(currentPagePath))
                {
                    // Disallow the use sibling page routing, a Razor page specific feature, from a non-page action.
                    // OR - this is a call from LinkGenerator where the HttpContext was not specified.
                    //
                    // We can't use a relative path in either case, because we don't know the base path.
                    throw new InvalidOperationException(Resources.FormatUrlHelper_RelativePagePathIsNotSupported(
                        pageName,
                        nameof(LinkGenerator),
                        nameof(HttpContext)));
                }

                return ViewEnginePath.CombinePath(currentPagePath, pageName);
            }

            return pageName;
        }

        // for unit testing
        internal static void AppendPathAndFragment(StringBuilder builder, PathString pathBase, string virtualPath, string fragment)
        {
            if (!pathBase.HasValue)
            {
                if (virtualPath.Length == 0)
                {
                    builder.Append("/");
                }
                else
                {
                    if (!virtualPath.StartsWith("/", StringComparison.Ordinal))
                    {
                        builder.Append("/");
                    }

                    builder.Append(virtualPath);
                }
            }
            else
            {
                if (virtualPath.Length == 0)
                {
                    builder.Append(pathBase.Value);
                }
                else
                {
                    builder.Append(pathBase.Value);

                    if (pathBase.Value.EndsWith("/", StringComparison.Ordinal))
                    {
                        builder.Length--;
                    }

                    if (!virtualPath.StartsWith("/", StringComparison.Ordinal))
                    {
                        builder.Append("/");
                    }

                    builder.Append(virtualPath);
                }
            }

            if (!string.IsNullOrEmpty(fragment))
            {
                builder.Append("#").Append(fragment);
            }
        }

        private bool TryFastGenerateUrl(
            string protocol,
            string host,
            string virtualPath,
            string fragment,
            out string url)
        {
            var pathBase = ActionContext.HttpContext.Request.PathBase;
            url = null;

            if (string.IsNullOrEmpty(protocol)
                && string.IsNullOrEmpty(host)
                && string.IsNullOrEmpty(fragment)
                && !pathBase.HasValue)
            {
                if (virtualPath.Length == 0)
                {
                    url = "/";
                    return true;
                }
                else if (virtualPath.StartsWith("/", StringComparison.Ordinal))
                {
                    url = virtualPath;
                    return true;
                }
            }

            return false;
        }

        private StringBuilder GetStringBuilder()
        {
            if (_stringBuilder == null)
            {
                _stringBuilder = new StringBuilder();
            }

            return _stringBuilder;
        }
    }
}
