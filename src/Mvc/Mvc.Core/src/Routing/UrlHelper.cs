// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// An implementation of <see cref="IUrlHelper"/> that contains methods to
/// build URLs for ASP.NET MVC within an application.
/// </summary>
public class UrlHelper : UrlHelperBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UrlHelper"/> class using the specified
    /// <paramref name="actionContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> for the current request.</param>
    public UrlHelper(ActionContext actionContext)
        : base(actionContext)
    {
    }

    /// <summary>
    /// Gets the <see cref="Http.HttpContext"/> associated with the current request.
    /// </summary>
    protected HttpContext HttpContext => ActionContext.HttpContext;

    /// <summary>
    /// Gets the top-level <see cref="IRouter"/> associated with the current request. Generally an
    /// <see cref="IRouteCollection"/> implementation.
    /// </summary>
    protected IRouter Router
    {
        get
        {
            var routers = ActionContext.RouteData.Routers;
            if (routers.Count == 0)
            {
                throw new InvalidOperationException("Could not find an IRouter associated with the ActionContext. "
                    + "If your application is using endpoint routing then you can get a IUrlHelperFactory with "
                    + "dependency injection and use it to create a UrlHelper, or use Microsoft.AspNetCore.Routing.LinkGenerator.");
            }

            return routers[0];
        }
    }

    /// <inheritdoc />
    public override string? Action(UrlActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        var valuesDictionary = GetValuesDictionary(actionContext.Values);

        NormalizeRouteValuesForAction(actionContext.Action, actionContext.Controller, valuesDictionary, AmbientValues);

        var virtualPathData = GetVirtualPathData(routeName: null, values: valuesDictionary);
        return GenerateUrl(actionContext.Protocol, actionContext.Host, virtualPathData, actionContext.Fragment);
    }

    /// <inheritdoc />
    public override string? RouteUrl(UrlRouteContext routeContext)
    {
        ArgumentNullException.ThrowIfNull(routeContext);

        var valuesDictionary = routeContext.Values as RouteValueDictionary ?? GetValuesDictionary(routeContext.Values);
        var virtualPathData = GetVirtualPathData(routeContext.RouteName, valuesDictionary);
        return GenerateUrl(routeContext.Protocol, routeContext.Host, virtualPathData, routeContext.Fragment);
    }

    /// <summary>
    /// Gets the <see cref="VirtualPathData"/> for the specified <paramref name="routeName"/> and route
    /// <paramref name="values"/>.
    /// </summary>
    /// <param name="routeName">The name of the route that is used to generate the <see cref="VirtualPathData"/>.
    /// </param>
    /// <param name="values">
    /// The <see cref="RouteValueDictionary"/>. The <see cref="Router"/> uses these values, in combination with
    /// <see cref="UrlHelperBase.AmbientValues"/>, to generate the URL.
    /// </param>
    /// <returns>The <see cref="VirtualPathData"/>.</returns>
    protected virtual VirtualPathData? GetVirtualPathData(string? routeName, RouteValueDictionary values)
    {
        var context = new VirtualPathContext(HttpContext, AmbientValues, values, routeName);
        return Router.GetVirtualPath(context);
    }

    /// <summary>
    /// Generates the URL using the specified components.
    /// </summary>
    /// <param name="protocol">The protocol for the URL, such as "http" or "https".</param>
    /// <param name="host">The host name for the URL.</param>
    /// <param name="pathData">The <see cref="VirtualPathData"/>.</param>
    /// <param name="fragment">The fragment for the URL.</param>
    /// <returns>The generated URL.</returns>
    protected virtual string? GenerateUrl(string? protocol, string? host, VirtualPathData? pathData, string? fragment)
    {
        return GenerateUrl(protocol, host, pathData?.VirtualPath, fragment);
    }
}
