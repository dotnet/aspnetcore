// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Represents a discovered component.
/// </summary>
internal class ComponentBuilder : IEquatable<ComponentBuilder?>
{
    /// <summary>
    /// Gets or sets the assembly name where this component comes from.
    /// </summary>
    public required string AssemblyName { get; set; }

    /// <summary>
    /// Gets or sets the component type.
    /// </summary>
    public required Type ComponentType { get; set; }

    /// <summary>
    /// Gets or sets the render mode for the component.
    /// </summary>
    public RenderModeAttribute? RenderMode { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ComponentBuilder);
    }

    /// <inheritdoc/>
    public bool Equals(ComponentBuilder? other)
    {
        return other is not null &&
               AssemblyName == other.AssemblyName &&
               EqualityComparer<Type>.Default.Equals(ComponentType, other.ComponentType);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(AssemblyName, ComponentType);
    }

    internal bool HasSource(string name)
    {
        return string.Equals(AssemblyName, name, StringComparison.Ordinal);
    }

    internal ComponentInfo Build()
    {
        if (RenderMode != null)
        {
            return new ComponentInfo(ComponentType)
            {
                RenderMode = RenderMode.Mode,
            };
        }
        else
        {
            return new ComponentInfo(ComponentType);
        }
    }
}
