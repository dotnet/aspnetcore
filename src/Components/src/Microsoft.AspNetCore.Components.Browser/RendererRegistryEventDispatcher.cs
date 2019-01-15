// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System;

namespace Microsoft.AspNetCore.Components.Browser
{
    /// <summary>
    /// Provides mechanisms for dispatching events to components in a <see cref="Renderer"/>.
    /// </summary>
    public static class RendererRegistryEventDispatcher
    {
        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(DispatchEvent))]
        public static void DispatchEvent(
            BrowserEventDescriptor eventDescriptor, string eventArgsJson)
        {
            var eventArgs = ParseEventArgsJson(eventDescriptor.EventArgsType, eventArgsJson);
            var renderer = RendererRegistry.Current.Find(eventDescriptor.BrowserRendererId);
            renderer.DispatchEvent(
                eventDescriptor.ComponentId,
                eventDescriptor.EventHandlerId,
                eventArgs);
        }

        private static UIEventArgs ParseEventArgsJson(string eventArgsType, string eventArgsJson)
        {
            switch (eventArgsType)
            {
                case "change":
                    return Json.Deserialize<UIChangeEventArgs>(eventArgsJson);
                case "clipboard":
                    return Json.Deserialize<UIClipboardEventArgs>(eventArgsJson);
                case "drag":
                    return Json.Deserialize<UIDragEventArgs>(eventArgsJson);
                case "error":
                    return Json.Deserialize<UIErrorEventArgs>(eventArgsJson);
                case "focus":
                    return Json.Deserialize<UIFocusEventArgs>(eventArgsJson);
                case "keyboard":
                    return Json.Deserialize<UIKeyboardEventArgs>(eventArgsJson);
                case "mouse":
                    return Json.Deserialize<UIMouseEventArgs>(eventArgsJson);
                case "pointer":
                    return Json.Deserialize<UIPointerEventArgs>(eventArgsJson);
                case "progress":
                    return Json.Deserialize<UIProgressEventArgs>(eventArgsJson);
                case "touch":
                    return Json.Deserialize<UITouchEventArgs>(eventArgsJson);
                case "unknown":
                    return Json.Deserialize<UIEventArgs>(eventArgsJson);
                case "wheel":
                    return Json.Deserialize<UIWheelEventArgs>(eventArgsJson);
                default:
                     throw new ArgumentException($"Unsupported value '{eventArgsType}'.", nameof(eventArgsType));
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        public class BrowserEventDescriptor
        {
            /// <summary>
            /// For framework use only.
            /// </summary>
            public int BrowserRendererId { get; set; }

            /// <summary>
            /// For framework use only.
            /// </summary>
            public int ComponentId { get; set; }

            /// <summary>
            /// For framework use only.
            /// </summary>
            public int EventHandlerId { get; set; }

            /// <summary>
            /// For framework use only.
            /// </summary>
            public string EventArgsType { get; set; }
        }
    }
}
