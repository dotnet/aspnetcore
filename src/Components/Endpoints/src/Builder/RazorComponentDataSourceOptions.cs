// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Options associated with the endpoints defined by the components in the
/// given <see cref="RazorComponentsEndpointRouteBuilderExtensions.MapRazorComponents{TRootComponent}(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)"/>
/// invocation.
/// </summary>
public class RazorComponentDataSourceOptions
{
    /// <summary>
    /// Gets or sets whether to automatically wire up the necessary endpoints
    /// based on the declared render modes of the components that are
    /// part of this set of endpoints.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>.
    /// </remarks>
    public bool UseDeclaredRenderModes { get; set; } = true;

    internal IList<IComponentRenderMode> ConfiguredRenderModes { get; } = new List<IComponentRenderMode>();
}
