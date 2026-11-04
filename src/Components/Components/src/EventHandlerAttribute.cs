// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Associates an event argument type with an event attribute name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class EventHandlerAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="EventHandlerAttribute"/>.
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="eventArgsType"></param>
    public EventHandlerAttribute(string attributeName, Type eventArgsType) : this(attributeName, eventArgsType, false, false)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="EventHandlerAttribute"/>.
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="eventArgsType"></param>
    /// <param name="enableStopPropagation"></param>
    /// <param name="enablePreventDefault"></param>
    public EventHandlerAttribute(string attributeName, Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault)
        : this(attributeName, eventArgsType, enableStopPropagation, enablePreventDefault, EventThrottleMode.Leading)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="EventHandlerAttribute"/>.
    /// </summary>
    /// <param name="attributeName">The attribute name.</param>
    /// <param name="eventArgsType">The event argument type.</param>
    /// <param name="enableStopPropagation">Whether the event supports stopping propagation.</param>
    /// <param name="enablePreventDefault">Whether the event supports preventing default behavior.</param>
    /// <param name="throttleMode">The throttling behavior for the event.</param>
    public EventHandlerAttribute(string attributeName, Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault, EventThrottleMode throttleMode)
    {
        ArgumentNullException.ThrowIfNull(attributeName);
        ArgumentNullException.ThrowIfNull(eventArgsType);

        AttributeName = attributeName;
        EventArgsType = eventArgsType;
        EnableStopPropagation = enableStopPropagation;
        EnablePreventDefault = enablePreventDefault;
        ThrottleMode = throttleMode;
    }

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// Gets the event argument type.
    /// </summary>
    public Type EventArgsType { get; }

    /// <summary>
    /// Gets the event's ability to stop propagation.
    /// </summary>
    public bool EnableStopPropagation { get; }

    /// <summary>
    /// Gets the event's ability to prevent default event flow.
    /// </summary>
    public bool EnablePreventDefault { get; }

    /// <summary>
    /// Gets the throttling behavior for the event.
    /// </summary>
    public EventThrottleMode ThrottleMode { get; }
}
