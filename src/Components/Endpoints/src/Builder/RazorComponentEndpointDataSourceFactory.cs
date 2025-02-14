// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Routing;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal class RazorComponentEndpointDataSourceFactory(
    RazorComponentEndpointFactory factory,
    IEnumerable<RenderModeEndpointProvider> providers,
    HotReloadService? hotReloadService = null)
{
    public RazorComponentEndpointDataSource<TRootComponent> CreateDataSource<[DynamicallyAccessedMembers(Component)] TRootComponent>(IEndpointRouteBuilder endpoints)
    {
        var builder = ComponentApplicationBuilder.GetBuilder<TRootComponent>() ??
            DefaultRazorComponentApplication<TRootComponent>.Instance.GetBuilder();

        return new RazorComponentEndpointDataSource<TRootComponent>(builder, providers, endpoints, factory, hotReloadService);
    }
}
