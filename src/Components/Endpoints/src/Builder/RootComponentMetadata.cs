// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
        AcceptsPageRenderModeParameter = TypeAcceptsPageRenderModeParameter(rootComponentType);
    }

    /// <summary>
    /// Gets the component type.
    /// </summary>
    [DynamicallyAccessedMembers(Component)]
    public Type Type { get; }

    internal bool AcceptsPageRenderModeParameter { get; }

    private static bool TypeAcceptsPageRenderModeParameter([DynamicallyAccessedMembers(Component)] Type type)
    {
        // While we could declare an interface or base class to indicate the component accepts PageRenderMode,
        // we don't want to force base classes in general, and the interface would purely duplicate information
        // we already infer via reflection in other places.
        var propertyInfo = type.GetProperty("PageRenderMode", BindingFlags.Instance | BindingFlags.Public);
        return propertyInfo is { CanWrite: true }
            && propertyInfo.PropertyType == typeof(IComponentRenderMode)
            && propertyInfo.GetCustomAttribute<ParameterAttribute>() is not null;
    }
}
