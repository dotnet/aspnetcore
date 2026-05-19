// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

internal sealed class WebEventData
{
    // This class represents the second half of parsing incoming event data,
    // once the event ID (and possibly the type of the eventArgs) becomes known.
    public static WebEventData Parse(
        Renderer renderer,
        JsonSerializerOptions jsonSerializerOptions,
        JsonElement eventDescriptorJson,
        JsonElement eventArgsJson)
    {
        WebEventDescriptor eventDescriptor;
        try
        {
            eventDescriptor = WebEventDescriptorReader.Read(eventDescriptorJson);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Error parsing the event descriptor", e);
        }

        return Parse(renderer, jsonSerializerOptions, eventDescriptor, eventArgsJson);
    }

    public static WebEventData Parse(
        Renderer renderer,
        JsonSerializerOptions jsonSerializerOptions,
        WebEventDescriptor eventDescriptor,
        JsonElement eventArgsJson)
    {
        var parsedEventArgs = ParseEventArgsJson(renderer, jsonSerializerOptions, eventDescriptor.EventHandlerId, eventDescriptor.EventName, eventArgsJson);
        return new WebEventData(
            eventDescriptor.EventHandlerId,
            eventDescriptor.EventFieldInfo,
            parsedEventArgs);
    }

    private WebEventData(ulong eventHandlerId, EventFieldInfo? eventFieldInfo, EventArgs eventArgs)
    {
        EventHandlerId = eventHandlerId;
        EventFieldInfo = eventFieldInfo;
        EventArgs = eventArgs;
    }

    public ulong EventHandlerId { get; }

    public EventFieldInfo? EventFieldInfo { get; }

    public EventArgs EventArgs { get; }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "We are already using the appropriate overload")]
    private static EventArgs ParseEventArgsJson(
        Renderer renderer,
        JsonSerializerOptions jsonSerializerOptions,
        ulong eventHandlerId,
        string eventName,
        JsonElement eventArgsJson)
    {
        try
        {
            if (TryDeserializeStandardWebEventArgs(eventName, eventArgsJson, out var eventArgs))
            {
                return eventArgs;
            }

            // For custom events, the args type is determined from the associated delegate
            var eventArgsType = renderer.GetEventArgsType(eventHandlerId);
            return (EventArgs)JsonSerializer.Deserialize(eventArgsJson.GetRawText(), eventArgsType, jsonSerializerOptions)!;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"There was an error parsing the event arguments. EventId: '{eventHandlerId}'.", e);
        }
    }

    private static bool TryDeserializeStandardWebEventArgs(
        string eventName,
        JsonElement eventArgsJson,
        [NotNullWhen(true)] out EventArgs? eventArgs)
    {
        // For back-compatibility, we recognize the built-in list of web event names and hard-code
        // rules about the deserialization type for their eventargs. This makes it possible to declare
        // an event handler as receiving EventArgs, and have it actually receive a subclass at runtime
        // depending on the event that was raised.
        //
        // The following list should remain in sync with EventArgsFactory.ts.

        switch (eventName)
        {
            case "input":
            case "change":
                // Special case for ChangeEventArgs because its value type can be one of
                // several types, and System.Text.Json doesn't pick types dynamically
                eventArgs = ChangeEventArgsReader.Read(eventArgsJson);
                return true;

            case "copy":
            case "cut":
            case "paste":
                eventArgs = ClipboardEventArgsReader.Read(eventArgsJson);
                return true;

            case "drag":
            case "dragend":
            case "dragenter":
            case "dragleave":
            case "dragover":
            case "dragstart":
            case "drop":
                eventArgs = DragEventArgsReader.Read(eventArgsJson);
                return true;

            case "focus":
            case "blur":
            case "focusin":
            case "focusout":
                eventArgs = FocusEventArgsReader.Read(eventArgsJson);
                return true;

            case "keydown":
            case "keyup":
            case "keypress":
                eventArgs = KeyboardEventArgsReader.Read(eventArgsJson);
                return true;

            case "contextmenu":
            case "click":
            case "mouseover":
            case "mouseout":
            case "mousemove":
            case "mousedown":
            case "mouseup":
            case "dblclick":
                eventArgs = MouseEventArgsReader.Read(eventArgsJson);
                return true;

            case "error":
                eventArgs = ErrorEventArgsReader.Read(eventArgsJson);
                return true;

            case "loadstart":
            case "timeout":
            case "abort":
            case "load":
            case "loadend":
            case "progress":
                eventArgs = ProgressEventArgsReader.Read(eventArgsJson);
                return true;

            case "touchcancel":
            case "touchend":
            case "touchmove":
            case "touchenter":
            case "touchleave":
            case "touchstart":
                eventArgs = TouchEventArgsReader.Read(eventArgsJson);
                return true;

            case "gotpointercapture":
            case "lostpointercapture":
            case "pointercancel":
            case "pointerdown":
            case "pointerenter":
            case "pointerleave":
            case "pointermove":
            case "pointerout":
            case "pointerover":
            case "pointerup":
                eventArgs = PointerEventArgsReader.Read(eventArgsJson);
                return true;

            case "wheel":
            case "mousewheel":
                eventArgs = WheelEventArgsReader.Read(eventArgsJson);
                return true;

            case "cancel":
            case "close":
            case "toggle":
                eventArgs = EventArgs.Empty;
                return true;

            default:
                // For custom event types, there are no built-in rules, so the deserialization type is
                // determined by the parameter declared on the delegate.
                eventArgs = null;
                return false;
        }
    }
}
