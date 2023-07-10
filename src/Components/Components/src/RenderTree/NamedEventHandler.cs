// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
/// of the Blazor framework. These types will change in a future release.
/// </summary>
public readonly struct NamedValue
{
    /// <summary>
    /// The ID of the component holding the named value.
    /// </summary>
    public readonly int ComponentId { get; }

    /// <summary>
    /// The index of the <see cref="RenderTreeFrameType.NamedValue"/> frame within the component's current render output.
    /// </summary>
    public readonly int FrameIndex { get; }

    /// <summary>
    /// The name-value pair's name.
    /// </summary>
    public readonly string Name { get; }

    /// <summary>
    /// The name-value pair's value.
    /// </summary>
    public readonly object? Value { get; }

    /// <summary>
    /// Constructs an instance of <see cref="NamedValue"/>.
    /// </summary>
    /// <param name="componentId">The ID of the component holding the named value.</param>
    /// <param name="frameIndex">The index of the <see cref="RenderTreeFrameType.NamedValue"/> frame within the component's current render output.</param>
    /// <param name="name">The name-value pair's name.</param>
    /// <param name="value">The name-value pair's value.</param>
    public NamedValue(int componentId, int frameIndex, string name, object? value)
    {
        ComponentId = componentId;
        FrameIndex = frameIndex;
        Name = name;
        Value = value;
    }
}
