// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Metadata that represents the component associated with an endpoint.
/// </summary>
public sealed class ComponentTypeMetadata
{
    /// <summary>
    /// Initializes a new instance of <see cref="ComponentTypeMetadata"/>.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    public ComponentTypeMetadata([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        Type = componentType;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ComponentTypeMetadata"/>.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="isStaticPage">A flag indicating whether the page's route is declared as static.</param>
    public ComponentTypeMetadata([DynamicallyAccessedMembers(Component)] Type componentType, bool isStaticPage)
        : this(componentType)
    {
        IsStaticPage = isStaticPage;
    }

    /// <summary>
    /// Gets the component type.
    /// </summary>
    [DynamicallyAccessedMembers(Component)]
    public Type Type { get; }

    /// <summary>
    /// Gets a flag indicating whether the page's route is declared as static.
    /// </summary>
    public bool IsStaticPage { get; }
}
