// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// An implementation of <see cref="IUrlHelper"/> that uses <see cref="LinkGenerator"/> to build URLs
/// for ASP.NET MVC within an application.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(EndpointRoutingUrlHelperDebugView))]
internal sealed class EndpointRoutingUrlHelper : UrlHelperBase
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly LinkGenerator _linkGenerator;

    public EndpointRoutingUrlHelper(
        ActionContext actionContext,
        LinkGenerator linkGenerator,
        EndpointDataSource endpointDataSource)
        : base(actionContext)
    {
        ArgumentNullException.ThrowIfNull(linkGenerator);
        ArgumentNullException.ThrowIfNull(endpointDataSource);

        _linkGenerator = linkGenerator;
        _endpointDataSource = endpointDataSource;
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

    private string DebuggerToString() => $"Endpoints = {_endpointDataSource.Endpoints.Count}";

    private sealed class EndpointRoutingUrlHelperDebugView(EndpointRoutingUrlHelper helper)
    {
        private readonly EndpointRoutingUrlHelper _helper = helper;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Endpoint[] Items => _helper._endpointDataSource.Endpoints.ToArray();
    }
}
