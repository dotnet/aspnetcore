// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Metadata that represents the root component associated with an endpoint.
/// </summary>
public sealed class RootComponentMetadata
{
    /// <summary>
    /// Initializes a new instance of <see cref="RootComponentMetadata"/>.
    /// </summary>
    /// <param name="rootComponentType">The component type.</param>
    public RootComponentMetadata([DynamicallyAccessedMembers(Component)] Type rootComponentType)
    {
        Type = rootComponentType;
    }

    /// <summary>
    /// Gets the component type.
    /// </summary>
    [DynamicallyAccessedMembers(Component)]
    public Type Type { get; }
}
