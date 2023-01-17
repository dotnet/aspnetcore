// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// An implementation of <see cref="IUrlHelper"/> that uses <see cref="LinkGenerator"/> to build URLs
/// for ASP.NET MVC within an application.
/// </summary>
internal sealed class EndpointRoutingUrlHelper : UrlHelperBase
{
    private readonly ILogger<EndpointRoutingUrlHelper> _logger;
    private readonly LinkGenerator _linkGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointRoutingUrlHelper"/> class using the specified
    /// <paramref name="actionContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="Mvc.ActionContext"/> for the current request.</param>
    /// <param name="linkGenerator">The <see cref="LinkGenerator"/> used to generate the link.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public EndpointRoutingUrlHelper(
        ActionContext actionContext,
        LinkGenerator linkGenerator,
        ILogger<EndpointRoutingUrlHelper> logger)
        : base(actionContext)
    {
        ArgumentNullException.ThrowIfNull(linkGenerator);
        ArgumentNullException.ThrowIfNull(logger);

        _linkGenerator = linkGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public override string? Action(UrlActionContext urlActionContext)
    {
        ArgumentNullException.ThrowIfNull(urlActionContext);

        var values = GetValuesDictionary(urlActionContext.Values);

        if (urlActionContext.Action == null)
        {
            if (!values.ContainsKey("action") &&
                AmbientValues.TryGetValue("action", out var action))
            {
                values["action"] = action;
            }
        }
        else
        {
            values["action"] = urlActionContext.Action;
        }

        if (urlActionContext.Controller == null)
        {
            if (!values.ContainsKey("controller") &&
                AmbientValues.TryGetValue("controller", out var controller))
            {
                values["controller"] = controller;
            }
        }
        else
        {
            values["controller"] = urlActionContext.Controller;
        }

        var path = _linkGenerator.GetPathByRouteValues(
            ActionContext.HttpContext,
            routeName: null,
            values,
            fragment: urlActionContext.Fragment == null ? FragmentString.Empty : new FragmentString("#" + urlActionContext.Fragment));
        return GenerateUrl(urlActionContext.Protocol, urlActionContext.Host, path);
    }

    /// <inheritdoc />
    public override string? RouteUrl(UrlRouteContext routeContext)
    {
        ArgumentNullException.ThrowIfNull(routeContext);

        var path = _linkGenerator.GetPathByRouteValues(
            ActionContext.HttpContext,
            routeContext.RouteName,
            routeContext.Values,
            fragment: routeContext.Fragment == null ? FragmentString.Empty : new FragmentString("#" + routeContext.Fragment));
        return GenerateUrl(routeContext.Protocol, routeContext.Host, path);
    }
}
