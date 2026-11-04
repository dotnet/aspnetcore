// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Provides methods for building a collection of <see cref="RenderTreeFrame"/> entries.
/// </summary>
public static class WebRenderTreeBuilderExtensions
{
    // The "prevent default" and "stop propagation" flags behave like attributes, in that:
    // - you can have multiple of them on a given element (for separate events)
    // - you can add and remove them dynamically
    // - they are independent of other attributes (e.g., you can "stop propagation" of a given
    //   event type on an element that doesn't itself have a handler for that event)
    // As such, they are represented as attributes to give the right diffing behavior.
    //
    // As a private implementation detail, their internal representation is magic-named
    // attributes. This may change in the future. If we add support for multiple-same
    // -named-attributes-per-element (#14365), then we will probably also declare a new
    // AttributeType concept, and have specific attribute types for these flags, and
    // the "name" can simply be the name of the event being modified.

    /// <summary>
    /// Appends a frame representing an instruction to prevent the default action
    /// for a specified event.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="eventName">The name of the event to be affected.</param>
    /// <param name="value">True if the default action is to be prevented, otherwise false.</param>
    public static void AddEventPreventDefaultAttribute(this RenderTreeBuilder builder, int sequence, string eventName, bool value)
    {
        builder.AddAttribute(sequence, $"__internal_preventDefault_{eventName}", value);
    }

    /// <summary>
    /// Appends a frame representing an instruction to stop the specified event from
    /// propagating beyond the current element.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="eventName">The name of the event to be affected.</param>
    /// <param name="value">True if propagation should be stopped here, otherwise false.</param>
    public static void AddEventStopPropagationAttribute(this RenderTreeBuilder builder, int sequence, string eventName, bool value)
    {
        builder.AddAttribute(sequence, $"__internal_stopPropagation_{eventName}", value);
    }

    /// <summary>
    /// Appends a frame representing an instruction to debounce the specified event by the
    /// given interval in milliseconds.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="eventName">The name of the event to be affected.</param>
    /// <param name="value">The debounce interval in milliseconds, or a value less than or equal to zero to disable debouncing.</param>
    public static void AddEventDebounceAttribute(this RenderTreeBuilder builder, int sequence, string eventName, int value)
    {
        builder.AddAttribute(
            sequence,
            $"__internal_debounce_{eventName}",
            value > 0 ? value.ToString(CultureInfo.InvariantCulture) : null);
    }

    /// <summary>
    /// Appends a frame representing an instruction to throttle the specified event by the
    /// given interval in milliseconds.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="eventName">The name of the event to be affected.</param>
    /// <param name="value">The throttle interval in milliseconds, or a value less than or equal to zero to disable throttling.</param>
    /// <param name="throttleMode">The throttling mode to apply when the interval is positive.</param>
    public static void AddEventThrottleAttribute(this RenderTreeBuilder builder, int sequence, string eventName, int value, EventThrottleMode throttleMode = EventThrottleMode.Leading)
    {
        string? attributeValue = null;
        if (value > 0)
        {
            var intervalText = value.ToString(CultureInfo.InvariantCulture);
            attributeValue = throttleMode == EventThrottleMode.LeadingAndTrailing
                ? $"{intervalText}:trailing"
                : intervalText;
        }

        builder.AddAttribute(sequence, $"__internal_throttle_{eventName}", attributeValue);
    }
}
