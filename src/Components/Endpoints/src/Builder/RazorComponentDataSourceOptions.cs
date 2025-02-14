// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Options associated with the endpoints defined by the components in the
/// given <see cref="RazorComponentsEndpointRouteBuilderExtensions.MapRazorComponents{TRootComponent}(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)"/>
/// invocation.
/// </summary>
internal class RazorComponentDataSourceOptions
{
    internal static readonly EqualityComparer<IComponentRenderMode> RenderModeComparer = EqualityComparer<IComponentRenderMode>
        .Create(
            equals: (x, y) => (x,y) switch
            {
                (InteractiveServerRenderMode, InteractiveServerRenderMode) => true,
                (InteractiveWebAssemblyRenderMode, InteractiveWebAssemblyRenderMode) => true,
                _ => false,
            },
            getHashCode: obj => obj switch
            {
                InteractiveServerRenderMode => 1,
                InteractiveWebAssemblyRenderMode => 2,
                _ => throw new InvalidOperationException($"Unknown render mode: {obj}"),
            });

    public string? ManifestPath { get; set; }

    internal ISet<IComponentRenderMode> ConfiguredRenderModes { get; } = new HashSet<IComponentRenderMode>(RenderModeComparer);
}
