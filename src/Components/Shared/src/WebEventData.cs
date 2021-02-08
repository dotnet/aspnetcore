// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web
{
    internal class WebEventData
    {
        // This class represents the second half of parsing incoming event data,
        // once the event ID (and possibly the type of the eventArgs) becomes known.
        public static WebEventData Parse(Renderer renderer, string eventDescriptorJson, string eventArgsJson)
        {
            WebEventDescriptor eventDescriptor;
            try
            {
                eventDescriptor = Deserialize<WebEventDescriptor>(eventDescriptorJson);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Error parsing the event descriptor", e);
            }

            return Parse(
                renderer,
                eventDescriptor,
                eventArgsJson);
        }

        public static WebEventData Parse(Renderer renderer, WebEventDescriptor eventDescriptor, string eventArgsJson)
        {
            var parsedEventArgs = ParseEventArgsJson(renderer, eventDescriptor.EventHandlerId, eventDescriptor.EventName, eventArgsJson);
            return new WebEventData(
                eventDescriptor.BrowserRendererId,
                eventDescriptor.EventHandlerId,
                InterpretEventFieldInfo(eventDescriptor.EventFieldInfo),
                parsedEventArgs);
        }

        private WebEventData(int browserRendererId, ulong eventHandlerId, EventFieldInfo? eventFieldInfo, EventArgs eventArgs)
        {
            BrowserRendererId = browserRendererId;
            EventHandlerId = eventHandlerId;
            EventFieldInfo = eventFieldInfo;
            EventArgs = eventArgs;
        }

        public int BrowserRendererId { get; }

        public ulong EventHandlerId { get; }

        public EventFieldInfo? EventFieldInfo { get; }

        public EventArgs EventArgs { get; }

        private static EventArgs ParseEventArgsJson(Renderer renderer, ulong eventHandlerId, string eventName, string eventArgsJson)
        {
            try
            {
                if (TryGetStandardWebEventArgsType(eventName, out var eventArgsType))
                {
                    // Special case for ChangeEventArgs because its value type can be one of
                    // several types, and System.Text.Json doesn't pick types dynamically
                    if (eventArgsType == typeof(ChangeEventArgs))
                    {
                        return DeserializeChangeEventArgs(eventArgsJson);
                    }
                }
                else
                {
                    // For custom events, the args type is determined from the associated delegate
                    eventArgsType = renderer.GetEventArgsType(eventHandlerId);
                }

                return (EventArgs)JsonSerializer.Deserialize(eventArgsJson, eventArgsType, JsonSerializerOptionsProvider.Options)!;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"There was an error parsing the event arguments. EventId: '{eventHandlerId}'.", e);
            }
        }

        private static bool TryGetStandardWebEventArgsType(string eventName, [MaybeNullWhen(false)] out Type type)
        {
            // For back-compatibility, we recognize the built-in list of web event names and hard-code
            // rules about the deserialization type for their eventargs. This makes it possible to declare
            // an event handler as receiving EventArgs, and have it actually receive a subclass at runtime
            // depending on the event that was raised.
            //
            // The following list should remain in sync with EventForDotNet.ts.

            switch (eventName)
            {
                case "input":
                case "change":
                    type = typeof(ChangeEventArgs);
                    return true;

                case "copy":
                case "cut":
                case "paste":
                    type = typeof(EventArgs);
                    return true;

                case "drag":
                case "dragend":
                case "dragenter":
                case "dragleave":
                case "dragover":
                case "dragstart":
                case "drop":
                    type = typeof(DragEventArgs);
                    return true;

                case "focus":
                case "blur":
                case "focusin":
                case "focusout":
                    type = typeof(EventArgs);
                    return true;

                case "keydown":
                case "keyup":
                case "keypress":
                    type = typeof(KeyboardEventArgs);
                    return true;

                case "contextmenu":
                case "click":
                case "mouseover":
                case "mouseout":
                case "mousemove":
                case "mousedown":
                case "mouseup":
                case "dblclick":
                    type = typeof(MouseEventArgs);
                    return true;

                case "error":
                    type = typeof(ErrorEventArgs);
                    return true;

                case "loadstart":
                case "timeout":
                case "abort":
                case "load":
                case "loadend":
                case "progress":
                    type = typeof(ProgressEventArgs);
                    return true;

                case "touchcancel":
                case "touchend":
                case "touchmove":
                case "touchenter":
                case "touchleave":
                case "touchstart":
                    type = typeof(TouchEventArgs);
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
                    type = typeof(PointerEventArgs);
                    return true;

                case "wheel":
                case "mousewheel":
                    type = typeof(WheelEventArgs);
                    return true;

                case "toggle":
                    type = typeof(EventArgs);
                    return true;

                default:
                    // For custom event types, there are no built-in rules, so the deserialization type is
                    // determined by the parameter declared on the delegate.
                    type = null;
                    return false;
            }
        }

        private static EventFieldInfo? InterpretEventFieldInfo(EventFieldInfo? fieldInfo)
        {
            // The incoming field value can be either a bool or a string, but since the .NET property
            // type is 'object', it will deserialize initially as a JsonElement
            if (fieldInfo?.FieldValue is JsonElement attributeValueJsonElement)
            {
                switch (attributeValueJsonElement.ValueKind)
                {
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return new EventFieldInfo
                        {
                            ComponentId = fieldInfo.ComponentId,
                            FieldValue = attributeValueJsonElement.GetBoolean()
                        };
                    default:
                        return new EventFieldInfo
                        {
                            ComponentId = fieldInfo.ComponentId,
                            FieldValue = attributeValueJsonElement.GetString()!
                        };
                }
            }

            return null;
        }

        static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptionsProvider.Options)!;

        private static ChangeEventArgs DeserializeChangeEventArgs(string eventArgsJson)
        {
            var changeArgs = Deserialize<ChangeEventArgs>(eventArgsJson);
            var jsonElement = (JsonElement)changeArgs.Value!;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Null:
                    changeArgs.Value = null;
                    break;
                case JsonValueKind.String:
                    changeArgs.Value = jsonElement.GetString();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    changeArgs.Value = jsonElement.GetBoolean();
                    break;
                default:
                    throw new ArgumentException($"Unsupported {nameof(ChangeEventArgs)} value {jsonElement}.");
            }
            return changeArgs;
        }
    }
}
