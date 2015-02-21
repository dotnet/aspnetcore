// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Link-related extensions for <see cref="IHtmlHelper"/>.
    /// </summary>
    public static class HtmlHelperLinkExtensions
    {
        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
        /// </summary>
        /// <param name="helper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: null,
                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
        /// </summary>
        /// <param name="helper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            object routeValues)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
        /// </summary>
        /// <param name="helper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            object routeValues,
            object htmlAttributes)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName: null,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
        /// </summary>
        /// <param name="helper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            string controllerName)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: null,
                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
        /// </summary>
        /// <param name="helper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            object routeValues)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified action.
        /// </summary>
        /// <param name="helper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="controllerName">The name of the controller.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString ActionLink(
            [NotNull] this IHtmlHelper helper,
            [NotNull] string linkText,
            string actionName,
            string controllerName,
            object routeValues,
            object htmlAttributes)
        {
            return helper.ActionLink(
                linkText,
                actionName,
                controllerName,
                protocol: null,
                hostname: null,
                fragment: null,
                routeValues: routeValues,
                htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified route.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            object routeValues)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName: null,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: routeValues,
                                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified route.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: null,
                                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified route.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName,
            object routeValues)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: routeValues,
                                htmlAttributes: null);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified route.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            object routeValues,
            object htmlAttributes)
        {
            return htmlHelper.RouteLink(
                                linkText,
                                routeName: null,
                                protocol: null,
                                hostName: null,
                                fragment: null,
                                routeValues: routeValues,
                                htmlAttributes: htmlAttributes);
        }

        /// <summary>
        /// Returns an anchor (&lt;a&gt;) element that contains a URL path to the specified route.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/> instance this method extends.</param>
        /// <param name="linkText">The inner text of the anchor element. Must not be <c>null</c>.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">
        /// An <see cref="object"/> that contains the parameters for a route. The parameters are retrieved through
        /// reflection by examining the properties of the <see cref="object"/>. This <see cref="object"/> is typically
        /// created using <see cref="object"/> initializer syntax. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the route
        /// parameters.
        /// </param>
        /// <param name="htmlAttributes">
        /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
        /// <see cref="System.Collections.Generic.IDictionary{string, object}"/> instance containing the HTML
        /// attributes.
        /// </param>
        /// <returns>A new <see cref="HtmlString"/> containing the anchor element.</returns>
        public static HtmlString RouteLink(
            [NotNull] this IHtmlHelper htmlHelper,
            [NotNull] string linkText,
            string routeName,
            object routeValues,
            object htmlAttributes)
        {
            return htmlHelper.RouteLink(
                                 linkText,
                                 routeName,
                                 protocol: null,
                                 hostName: null,
                                 fragment: null,
                                 routeValues: routeValues,
                                 htmlAttributes: htmlAttributes);
        }
    }
}
