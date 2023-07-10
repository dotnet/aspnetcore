// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
/// of the Blazor framework. These types will change in a future release.
/// </summary>
public readonly struct NamedEventHandler
{
    /// <summary>
    /// The event handler ID for the event.
    /// </summary>
    public readonly ulong EventHandlerId { get; }

    /// <summary>
    /// The event type, e.g., 'onsubmit'.
    /// </summary>
    public readonly string EventType { get; }

    /// <summary>
    /// The application-specified name for the event.
    /// </summary>
    public readonly string AssignedEventName { get; }

    /// <summary>
    /// Constructs an instanced of <see cref="NamedEventHandler"/>.
    /// </summary>
    /// <param name="eventHandlerId">The event handler ID for the event.</param>
    /// <param name="eventType">The event type, e.g., 'onsubmit'.</param>
    /// <param name="assignedEventName">The application-specified name for the event.</param>
    public NamedEventHandler(ulong eventHandlerId, string eventType, string assignedEventName)
    {
        EventHandlerId = eventHandlerId;
        EventType = eventType;
        AssignedEventName = assignedEventName;
    }
}
