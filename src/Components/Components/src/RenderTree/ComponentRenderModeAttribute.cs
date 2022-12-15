// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies how the component prefers to be rendered. This can be overridden at the point
/// of usage.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ComponentRenderModeAttribute : Attribute
{
    /// <summary>
    /// Gets the preferred render mode.
    /// </summary>
    public ComponentRenderMode Mode { get; }

    /// <summary>
    /// Constructs an instance of <see cref="ComponentRenderModeAttribute"/>.
    /// </summary>
    /// <param name="mode">The preferred render mode.</param>
    public ComponentRenderModeAttribute(ComponentRenderMode mode)
    {
        Mode = mode;
    }
}
