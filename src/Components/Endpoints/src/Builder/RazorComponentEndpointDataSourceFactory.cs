// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Routing;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal class RazorComponentEndpointDataSourceFactory(
    RazorComponentEndpointFactory factory,
    IEnumerable<RenderModeEndpointProvider> providers,
    IComponentTypeInfoResolver componentTypeInfoResolver,
    HotReloadService? hotReloadService = null)
{
    public RazorComponentEndpointDataSource<TRootComponent> CreateDataSource<[DynamicallyAccessedMembers(Component)] TRootComponent>(IEndpointRouteBuilder endpoints)
    {
        var dataSource = new RazorComponentEndpointDataSource<TRootComponent>(
            providers,
            endpoints,
            factory,
            componentTypeInfoResolver,
            hotReloadService);

        dataSource.ComponentApplicationBuilderActions.Add(builder =>
        {
            builder.AddAssembly(typeof(TRootComponent).Assembly);
        });

        return dataSource;
    }
}
