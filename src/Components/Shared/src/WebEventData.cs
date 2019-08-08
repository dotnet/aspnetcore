// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web
{
    internal class WebEventData
    {
        // This class represents the second half of parsing incoming event data,
        // once the type of the eventArgs becomes known.

        public static WebEventData Parse(string eventDescriptorJson, string eventArgsJson)
        {
            return Parse(
                Deserialize<WebEventDescriptor>(eventDescriptorJson),
                eventArgsJson);
        }

        public static WebEventData Parse(WebEventDescriptor eventDescriptor, string eventArgsJson)
        {
            return new WebEventData(
                eventDescriptor.BrowserRendererId,
                eventDescriptor.EventHandlerId,
                InterpretEventFieldInfo(eventDescriptor.EventFieldInfo),
                ParseEventArgsJson(eventDescriptor.EventArgsType, eventArgsJson));
        }

        private WebEventData(int browserRendererId, ulong eventHandlerId, EventFieldInfo eventFieldInfo, EventArgs eventArgs)
        {
            BrowserRendererId = browserRendererId;
            EventHandlerId = eventHandlerId;
            EventFieldInfo = eventFieldInfo;
            EventArgs = eventArgs;
        }

        public int BrowserRendererId { get; }

        public ulong EventHandlerId { get; }

        public EventFieldInfo EventFieldInfo { get; }

        public EventArgs EventArgs { get; }

        private static EventArgs ParseEventArgsJson(string eventArgsType, string eventArgsJson)
        {
            switch (eventArgsType)
            {
                case "change":
                    return DeserializeChangeEventArgs(eventArgsJson);
                case "clipboard":
                    return Deserialize<UIClipboardEventArgs>(eventArgsJson);
                case "drag":
                    return Deserialize<UIDragEventArgs>(eventArgsJson);
                case "error":
                    return Deserialize<UIErrorEventArgs>(eventArgsJson);
                case "focus":
                    return Deserialize<UIFocusEventArgs>(eventArgsJson);
                case "keyboard":
                    return Deserialize<UIKeyboardEventArgs>(eventArgsJson);
                case "mouse":
                    return Deserialize<UIMouseEventArgs>(eventArgsJson);
                case "pointer":
                    return Deserialize<UIPointerEventArgs>(eventArgsJson);
                case "progress":
                    return Deserialize<UIProgressEventArgs>(eventArgsJson);
                case "touch":
                    return Deserialize<UITouchEventArgs>(eventArgsJson);
                case "unknown":
                    return EventArgs.Empty;
                case "wheel":
                    return Deserialize<UIWheelEventArgs>(eventArgsJson);
                default:
                    throw new ArgumentException($"Unsupported value '{eventArgsType}'.", nameof(eventArgsType));
            }
        }

        private static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptionsProvider.Options);
        }

        private static EventFieldInfo InterpretEventFieldInfo(EventFieldInfo fieldInfo)
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
                            FieldValue = attributeValueJsonElement.GetString()
                        };
                }
            }

            return null;
        }

        private static ChangeEventArgs DeserializeChangeEventArgs(string eventArgsJson)
        {
            var changeArgs = Deserialize<ChangeEventArgs>(eventArgsJson);
            var jsonElement = (JsonElement)changeArgs.Value;
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
