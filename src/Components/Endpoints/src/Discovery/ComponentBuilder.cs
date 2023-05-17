// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a discovered component.
/// </summary>
public class ComponentBuilder : IEquatable<ComponentBuilder?>
{
    /// <summary>
    /// Gets or sets the source for the component.
    /// </summary>
    public required string Source { get; set; }

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
               Source == other.Source &&
               EqualityComparer<Type>.Default.Equals(ComponentType, other.ComponentType);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Source, ComponentType);
    }

    internal bool HasSource(string name)
    {
        return string.Equals(Source, name, StringComparison.Ordinal);
    }
}
