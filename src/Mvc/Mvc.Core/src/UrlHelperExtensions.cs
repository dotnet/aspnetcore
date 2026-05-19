// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Static class for url helper extension methods.
/// </summary>
public static class UrlHelperExtensions
{
    /// <summary>
    /// Generates a URL with a path for an action method.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <returns>The generated URL.</returns>
    public static string? Action(this IUrlHelper helper)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(
            action: null,
            controller: null,
            values: null,
            protocol: null,
            host: null,
            fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <returns>The generated URL.</returns>
    public static string? Action(this IUrlHelper helper, string? action)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(action, controller: null, values: null, protocol: null, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name and route <paramref name="values"/>.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <returns>The generated URL.</returns>
    public static string? Action(this IUrlHelper helper, string? action, object? values)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(action, controller: null, values: values, protocol: null, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> and <paramref name="controller"/> names.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="controller">The name of the controller.</param>
    /// <returns>The generated URL.</returns>
    public static string? Action(this IUrlHelper helper, string? action, string? controller)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(action, controller, values: null, protocol: null, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name, <paramref name="controller"/> name, and route <paramref name="values"/>.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="controller">The name of the controller.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <returns>The generated URL.</returns>
    public static string? Action(this IUrlHelper helper, string? action, string? controller, object? values)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(action, controller, values, protocol: null, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>, and
    /// <paramref name="protocol"/> to use. See the remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="controller">The name of the controller.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the value of <see cref="HttpRequest.Host"/> to populate the host section of the generated URI.
    /// Relying on the value of the current request can allow untrusted input to influence the resulting URI unless
    /// the <c>Host</c> header has been validated. See the deployment documentation for instructions on how to properly
    /// validate the <c>Host</c> header in your deployment environment.
    /// </para>
    /// </remarks>
    public static string? Action(
        this IUrlHelper helper,
        string? action,
        string? controller,
        object? values,
        string? protocol)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(action, controller, values, protocol, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>,
    /// <paramref name="protocol"/> to use, and <paramref name="host"/> name.
    /// Generates an absolute URL if the <paramref name="protocol"/> and <paramref name="host"/> are
    /// non-<c>null</c>. See the remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="controller">The name of the controller.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? Action(
        this IUrlHelper helper,
        string? action,
        string? controller,
        object? values,
        string? protocol,
        string? host)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.Action(action, controller, values, protocol, host, fragment: null);
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>,
    /// <paramref name="protocol"/> to use, <paramref name="host"/> name, and <paramref name="fragment"/>.
    /// Generates an absolute URL if the <paramref name="protocol"/> and <paramref name="host"/> are
    /// non-<c>null</c>. See the remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="controller">The name of the controller.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <param name="fragment">The fragment for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? Action(
        this IUrlHelper helper,
        string? action,
        string? controller,
        object? values,
        string? protocol,
        string? host,
        string? fragment)
    {
        ArgumentNullException.ThrowIfNull(helper);

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
    public static string? RouteUrl(this IUrlHelper helper, object? values)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.RouteUrl(routeName: null, values: values, protocol: null, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with an absolute path for the specified <paramref name="routeName"/>.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="routeName">The name of the route that is used to generate URL.</param>
    /// <returns>The generated URL.</returns>
    public static string? RouteUrl(this IUrlHelper helper, string? routeName)
    {
        ArgumentNullException.ThrowIfNull(helper);

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
    public static string? RouteUrl(this IUrlHelper helper, string? routeName, object? values)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.RouteUrl(routeName, values, protocol: null, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with an absolute path for the specified route <paramref name="routeName"/> and route
    /// <paramref name="values"/>, which contains the specified <paramref name="protocol"/> to use. See the
    /// remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="routeName">The name of the route that is used to generate URL.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the value of <see cref="HttpRequest.Host"/> to populate the host section of the generated URI.
    /// Relying on the value of the current request can allow untrusted input to influence the resulting URI unless
    /// the <c>Host</c> header has been validated. See the deployment documentation for instructions on how to properly
    /// validate the <c>Host</c> header in your deployment environment.
    /// </para>
    /// </remarks>
    public static string? RouteUrl(
        this IUrlHelper helper,
        string? routeName,
        object? values,
        string? protocol)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.RouteUrl(routeName, values, protocol, host: null, fragment: null);
    }

    /// <summary>
    /// Generates a URL with an absolute path for the specified route <paramref name="routeName"/> and route
    /// <paramref name="values"/>, which contains the specified <paramref name="protocol"/> to use and
    /// <paramref name="host"/> name. Generates an absolute URL if
    /// <see cref="UrlActionContext.Protocol"/> and <see cref="UrlActionContext.Host"/> are non-<c>null</c>.
    /// See the remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="routeName">The name of the route that is used to generate URL.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? RouteUrl(
        this IUrlHelper helper,
        string? routeName,
        object? values,
        string? protocol,
        string? host)
    {
        ArgumentNullException.ThrowIfNull(helper);

        return helper.RouteUrl(routeName, values, protocol, host, fragment: null);
    }

    /// <summary>
    /// Generates a URL with an absolute path for the specified route <paramref name="routeName"/> and route
    /// <paramref name="values"/>, which contains the specified <paramref name="protocol"/> to use,
    /// <paramref name="host"/> name and <paramref name="fragment"/>. Generates an absolute URL if
    /// <see cref="UrlActionContext.Protocol"/> and <see cref="UrlActionContext.Host"/> are non-<c>null</c>.
    /// See the remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="routeName">The name of the route that is used to generate URL.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <param name="fragment">The fragment for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? RouteUrl(
        this IUrlHelper helper,
        string? routeName,
        object? values,
        string? protocol,
        string? host,
        string? fragment)
    {
        ArgumentNullException.ThrowIfNull(helper);

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
    /// Generates a URL with a relative path for the specified <paramref name="pageName"/>.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <returns>The generated URL.</returns>
    public static string? Page(this IUrlHelper urlHelper, string? pageName)
        => Page(urlHelper, pageName, values: null);

    /// <summary>
    /// Generates a URL with a relative path for the specified <paramref name="pageName"/>.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <param name="pageHandler">The handler to generate the url for.</param>
    /// <returns>The generated URL.</returns>
    public static string? Page(this IUrlHelper urlHelper, string? pageName, string? pageHandler)
        => Page(urlHelper, pageName, pageHandler, values: null);

    /// <summary>
    /// Generates a URL with a relative path for the specified <paramref name="pageName"/>.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <returns>The generated URL.</returns>
    public static string? Page(this IUrlHelper urlHelper, string? pageName, object? values)
        => Page(urlHelper, pageName, pageHandler: null, values: values);

    /// <summary>
    /// Generates a URL with a relative path for the specified <paramref name="pageName"/>.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <param name="pageHandler">The handler to generate the url for.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <returns>The generated URL.</returns>
    public static string? Page(
        this IUrlHelper urlHelper,
        string? pageName,
        string? pageHandler,
        object? values)
        => Page(urlHelper, pageName, pageHandler, values, protocol: null);

    /// <summary>
    /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>. See the remarks section
    /// for important security information.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <param name="pageHandler">The handler to generate the url for.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the value of <see cref="HttpRequest.Host"/> to populate the host section of the generated URI.
    /// Relying on the value of the current request can allow untrusted input to influence the resulting URI unless
    /// the <c>Host</c> header has been validated. See the deployment documentation for instructions on how to properly
    /// validate the <c>Host</c> header in your deployment environment.
    /// </para>
    /// </remarks>
    public static string? Page(
        this IUrlHelper urlHelper,
        string? pageName,
        string? pageHandler,
        object? values,
        string? protocol)
        => Page(urlHelper, pageName, pageHandler, values, protocol, host: null, fragment: null);

    /// <summary>
    /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>. See the remarks section for
    /// important security information.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <param name="pageHandler">The handler to generate the url for.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? Page(
        this IUrlHelper urlHelper,
        string? pageName,
        string? pageHandler,
        object? values,
        string? protocol,
        string? host)
        => Page(urlHelper, pageName, pageHandler, values, protocol, host, fragment: null);

    /// <summary>
    /// Generates a URL with an absolute path for the specified <paramref name="pageName"/>. See the remarks section for
    /// important security information.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for.</param>
    /// <param name="pageHandler">The handler to generate the url for.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <param name="fragment">The fragment for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? Page(
        this IUrlHelper urlHelper,
        string? pageName,
        string? pageHandler,
        object? values,
        string? protocol,
        string? host,
        string? fragment)
    {
        ArgumentNullException.ThrowIfNull(urlHelper);

        var routeValues = new RouteValueDictionary(values);
        var ambientValues = urlHelper.ActionContext.RouteData.Values;

        UrlHelperBase.NormalizeRouteValuesForPage(urlHelper.ActionContext, pageName, pageHandler, routeValues, ambientValues);

        return urlHelper.RouteUrl(
            routeName: null,
            values: routeValues,
            protocol: protocol,
            host: host,
            fragment: fragment);
    }

    /// <summary>
    /// Generates an absolute URL for an action method, which contains the specified
    /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>,
    /// <paramref name="protocol"/> to use, <paramref name="host"/> name, and <paramref name="fragment"/>.
    /// Generates an absolute URL if the <paramref name="protocol"/> and <paramref name="host"/> are
    /// non-<c>null</c>. See the remarks section for important security information.
    /// </summary>
    /// <param name="helper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="action">The name of the action method. When <see langword="null" />, defaults to the current executing action.</param>
    /// <param name="controller">The name of the controller. When <see langword="null" />, defaults to the current executing controller.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <param name="fragment">The fragment for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? ActionLink(
        this IUrlHelper helper,
        string? action = null,
        string? controller = null,
        object? values = null,
        string? protocol = null,
        string? host = null,
        string? fragment = null)
    {
        ArgumentNullException.ThrowIfNull(helper);

        var httpContext = helper.ActionContext.HttpContext;

        if (protocol == null)
        {
            protocol = httpContext.Request.Scheme;
        }

        if (host == null)
        {
            host = httpContext.Request.Host.ToUriComponent();
        }

        return Action(helper, action, controller, values, protocol, host, fragment);
    }

    /// <summary>
    /// Generates an absolute URL for a page, which contains the specified
    /// <paramref name="pageName"/>, <paramref name="pageHandler"/>, route <paramref name="values"/>,
    /// <paramref name="protocol"/> to use, <paramref name="host"/> name, and <paramref name="fragment"/>.
    /// Generates an absolute URL if the <paramref name="protocol"/> and <paramref name="host"/> are
    /// non-<c>null</c>. See the remarks section for important security information.
    /// </summary>
    /// <param name="urlHelper">The <see cref="IUrlHelper"/>.</param>
    /// <param name="pageName">The page name to generate the url for. When <see langword="null"/>, defaults to the current executing page.</param>
    /// <param name="pageHandler">The handler to generate the url for. When <see langword="null"/>, defaults to the current executing handler.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <param name="fragment">The fragment for the URL.</param>
    /// <returns>The generated URL.</returns>
    /// <remarks>
    /// <para>
    /// The value of <paramref name="host"/> should be a trusted value. Relying on the value of the current request
    /// can allow untrusted input to influence the resulting URI unless the <c>Host</c> header has been validated.
    /// See the deployment documentation for instructions on how to properly validate the <c>Host</c> header in
    /// your deployment environment.
    /// </para>
    /// </remarks>
    public static string? PageLink(
        this IUrlHelper urlHelper,
        string? pageName = null,
        string? pageHandler = null,
        object? values = null,
        string? protocol = null,
        string? host = null,
        string? fragment = null)
    {
        ArgumentNullException.ThrowIfNull(urlHelper);

        var httpContext = urlHelper.ActionContext.HttpContext;

        if (protocol == null)
        {
            protocol = httpContext.Request.Scheme;
        }

        if (host == null)
        {
            host = httpContext.Request.Host.ToUriComponent();
        }

        return Page(urlHelper, pageName, pageHandler, values, protocol, host, fragment);
    }
}
