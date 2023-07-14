// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
/// of the Blazor framework. These types will change in a future release.
/// </summary>
/// <remarks>
/// Constructs an instance of <see cref="NamedEventChange"/>.
/// </remarks>
/// <param name="changeType">The type of the change.</param>
/// <param name="componentId">The ID of the component holding the named value.</param>
/// <param name="frameIndex">The index of the <see cref="RenderTreeFrameType.NamedEvent"/> frame within the component's current render output.</param>
/// <param name="eventType">The event type.</param>
/// <param name="assignedName">The application-assigned name.</param>
public readonly struct NamedEventChange(NamedEventChangeType changeType, int componentId, int frameIndex, string eventType, string assignedName)
{
    /// <summary>
    /// Describes the type of the change.
    /// </summary>
    public readonly NamedEventChangeType ChangeType { get; } = changeType;

    /// <summary>
    /// The ID of the component holding the named event.
    /// </summary>
    public readonly int ComponentId { get; } = componentId;

    /// <summary>
    /// The index of the <see cref="RenderTreeFrameType.NamedEvent"/> frame within the component's current render output.
    /// </summary>
    public readonly int FrameIndex { get; } = frameIndex;

    /// <summary>
    /// The event type.
    /// </summary>
    public readonly string EventType { get; } = eventType;

    /// <summary>
    /// The application-assigned name.
    /// </summary>
    public readonly string AssignedName { get; } = assignedName;
}
