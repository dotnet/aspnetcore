// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
/// of the Blazor framework. These types will change in a future release.
/// </summary>
//
// Describes a set of UI changes.
public readonly struct RenderBatch
{
    /// <summary>
    /// Gets the changes to components that were added or updated.
    /// </summary>
    public ArrayRange<RenderTreeDiff> UpdatedComponents { get; }

    /// <summary>
    /// Gets render frames that may be referenced by entries in <see cref="UpdatedComponents"/>.
    /// For example, edit entries of type <see cref="RenderTreeEditType.PrependFrame"/>
    /// will point to an entry in this array to specify the subtree to be prepended.
    /// </summary>
    public ArrayRange<RenderTreeFrame> ReferenceFrames { get; }

    /// <summary>
    /// Gets the IDs of the components that were disposed.
    /// </summary>
    public ArrayRange<int> DisposedComponentIDs { get; }

    /// <summary>
    /// Gets the IDs of the event handlers that were disposed.
    /// </summary>
    public ArrayRange<ulong> DisposedEventHandlerIDs { get; }

    /// <summary>
    /// Gets the named events that were changed, or null.
    /// </summary>
    public ArrayRange<NamedEventChange>? NamedEventChanges { get; }

    internal RenderBatch(
        ArrayRange<RenderTreeDiff> updatedComponents,
        ArrayRange<RenderTreeFrame> referenceFrames,
        ArrayRange<int> disposedComponentIDs,
        ArrayRange<ulong> disposedEventHandlerIDs,
        ArrayRange<NamedEventChange>? changedNamedEvents)
    {
        UpdatedComponents = updatedComponents;
        ReferenceFrames = referenceFrames;
        DisposedComponentIDs = disposedComponentIDs;
        DisposedEventHandlerIDs = disposedEventHandlerIDs;
        NamedEventChanges = changedNamedEvents;
    }
}

