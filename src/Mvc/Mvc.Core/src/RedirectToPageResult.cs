// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that returns a Found (302)
/// or Moved Permanently (301) response with a Location header.
/// Targets a registered route.
/// </summary>
public class RedirectToPageResult : ActionResult, IKeepTempDataResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The page to redirect to.</param>
    public RedirectToPageResult(string? pageName)
        : this(pageName, routeValues: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The page to redirect to.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    public RedirectToPageResult(string? pageName, string? pageHandler)
        : this(pageName, pageHandler, routeValues: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The page to redirect to.</param>
    /// <param name="routeValues">The parameters for the route.</param>
    public RedirectToPageResult(string? pageName, object? routeValues)
        : this(pageName, pageHandler: null, routeValues: routeValues, permanent: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The page to redirect to.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    /// <param name="routeValues">The parameters for the route.</param>
    public RedirectToPageResult(string? pageName, string? pageHandler, object? routeValues)
        : this(pageName, pageHandler, routeValues, permanent: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    /// <param name="routeValues">The parameters for the page.</param>
    /// <param name="permanent">If set to true, makes the redirect permanent (301). Otherwise a temporary redirect is used (302).</param>
    public RedirectToPageResult(
        string? pageName,
        string? pageHandler,
        object? routeValues,
        bool permanent)
        : this(pageName, pageHandler, routeValues, permanent, fragment: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values provided.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    /// <param name="routeValues">The parameters for the page.</param>
    /// <param name="permanent">If set to true, makes the redirect permanent (301). Otherwise a temporary redirect is used (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
    public RedirectToPageResult(
        string? pageName,
        string? pageHandler,
        object? routeValues,
        bool permanent,
        bool preserveMethod)
        : this(pageName, pageHandler, routeValues, permanent, preserveMethod, fragment: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    /// <param name="routeValues">The parameters for the route.</param>
    /// <param name="fragment">The fragment to add to the URL.</param>
    public RedirectToPageResult(
        string? pageName,
        string? pageHandler,
        object? routeValues,
        string? fragment)
        : this(pageName, pageHandler, routeValues, permanent: false, fragment: fragment)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    /// <param name="routeValues">The parameters for the page.</param>
    /// <param name="permanent">If set to true, makes the redirect permanent (301). Otherwise a temporary redirect is used (302).</param>
    /// <param name="fragment">The fragment to add to the URL.</param>
    public RedirectToPageResult(
        string? pageName,
        string? pageHandler,
        object? routeValues,
        bool permanent,
        string? fragment)
        : this(pageName, pageHandler, routeValues, permanent, preserveMethod: false, fragment: fragment)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectToPageResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="pageHandler">The page handler to redirect to.</param>
    /// <param name="routeValues">The parameters for the page.</param>
    /// <param name="permanent">If set to true, makes the redirect permanent (301). Otherwise a temporary redirect is used (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
    /// <param name="fragment">The fragment to add to the URL.</param>
    public RedirectToPageResult(
        string? pageName,
        string? pageHandler,
        object? routeValues,
        bool permanent,
        bool preserveMethod,
        string? fragment)
    {
        PageName = pageName;
        PageHandler = pageHandler;
        RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
        PreserveMethod = preserveMethod;
        Permanent = permanent;
        Fragment = fragment;
    }

    /// <summary>
    /// Gets or sets the <see cref="IUrlHelper" /> used to generate URLs.
    /// </summary>
    public IUrlHelper? UrlHelper { get; set; }

    /// <summary>
    /// Gets or sets the name of the page to route to.
    /// </summary>
    public string? PageName { get; set; }

    /// <summary>
    /// Gets or sets the page handler to redirect to.
    /// </summary>
    public string? PageHandler { get; set; }

    /// <summary>
    /// Gets or sets the route data to use for generating the URL.
    /// </summary>
    public RouteValueDictionary? RouteValues { get; set; }

    /// <summary>
    /// Gets or sets an indication that the redirect is permanent.
    /// </summary>
    public bool Permanent { get; set; }

    /// <summary>
    /// Gets or sets an indication that the redirect preserves the initial request method.
    /// </summary>
    public bool PreserveMethod { get; set; }

    /// <summary>
    /// Gets or sets the fragment to add to the URL.
    /// </summary>
    public string? Fragment { get; set; }

    /// <summary>
    /// Gets or sets the protocol for the URL, such as "http" or "https".
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Gets or sets the host name of the URL.
    /// </summary>
    public string? Host { get; set; }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<RedirectToPageResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
