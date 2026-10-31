// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
/// of the Blazor framework. These types will change in future release.
/// </summary>
//
// Describes changes to a component's render tree between successive renders.
public readonly struct RenderTreeDiff
{
    /// <summary>
    /// Gets the ID of the component.
    /// </summary>
    public readonly int ComponentId;

    /// <summary>
    /// Gets the changes to the render tree since a previous state.
    /// </summary>
    public readonly ArrayBuilderSegment<RenderTreeEdit> Edits;

    internal RenderTreeDiff(
        int componentId,
        ArrayBuilderSegment<RenderTreeEdit> entries)
    {
        ComponentId = componentId;
        Edits = entries;
    }
}
