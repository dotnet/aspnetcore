// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

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
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent ActionLink(
        this IHtmlHelper helper,
        string linkText,
        string actionName)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent ActionLink(
        this IHtmlHelper helper,
        string linkText,
        string actionName,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent ActionLink(
        this IHtmlHelper helper,
        string linkText,
        string actionName,
        object routeValues,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent ActionLink(
        this IHtmlHelper helper,
        string linkText,
        string actionName,
        string controllerName)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent ActionLink(
        this IHtmlHelper helper,
        string linkText,
        string actionName,
        string controllerName,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent ActionLink(
        this IHtmlHelper helper,
        string linkText,
        string actionName,
        string controllerName,
        object routeValues,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent RouteLink(
        this IHtmlHelper htmlHelper,
        string linkText,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent RouteLink(
        this IHtmlHelper htmlHelper,
        string linkText,
        string routeName)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent RouteLink(
        this IHtmlHelper htmlHelper,
        string linkText,
        string routeName,
        object routeValues)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent RouteLink(
        this IHtmlHelper htmlHelper,
        string linkText,
        object routeValues,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(linkText);

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
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the route
    /// parameters.
    /// </param>
    /// <param name="htmlAttributes">
    /// An <see cref="object"/> that contains the HTML attributes for the element. Alternatively, an
    /// <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance containing the HTML
    /// attributes.
    /// </param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the anchor element.</returns>
    public static IHtmlContent RouteLink(
        this IHtmlHelper htmlHelper,
        string linkText,
        string routeName,
        object routeValues,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(linkText);

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
