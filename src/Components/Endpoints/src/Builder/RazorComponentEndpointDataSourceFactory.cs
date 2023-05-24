// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal class RazorComponentEndpointDataSourceFactory
{
    private readonly RazorComponentEndpointFactory _factory;
    private readonly IEnumerable<RenderModeEndpointProvider> _providers;

    public RazorComponentEndpointDataSourceFactory(
        RazorComponentEndpointFactory factory,
        IEnumerable<RenderModeEndpointProvider> providers)
    {
        _factory = factory;
        _providers = providers;
    }

    public RazorComponentEndpointDataSource<TRootComponent> CreateDataSource<TRootComponent>(IEndpointRouteBuilder endpoints)
    {
        var builder = ComponentApplicationBuilder.GetBuilder<TRootComponent>() ??
            DefaultRazorComponentApplication<TRootComponent>.Instance.GetBuilder();

        return new RazorComponentEndpointDataSource<TRootComponent>(builder, _providers, endpoints.CreateApplicationBuilder(), _factory);
    }
}
