// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;
using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    /// <summary>
    /// Provides mechanisms for dispatching events to components in a <see cref="BrowserRenderer"/>.
    /// This is marked 'internal' because it only gets invoked from JS code.
    /// </summary>
    public static class BrowserRendererEventDispatcher
    {
        // TODO: Fix this for multi-user scenarios. Currently it doesn't stop people from
        // triggering events for other people by passing an arbitrary browserRendererId.
        //
        // Preferred fix: Instead of storing the Renderer instances in a static dictionary
        // store them within the context of a Circuit. Then we'll only look up the ones
        // associated with the caller's circuit. This takes care of ensuring they are
        // released when the circuit is closed too.
        //
        // More generally, we must move away from using statics for any per-user state
        // now that we have multi-user scenarios.

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(DispatchEvent))]
        public static void DispatchEvent(
            BrowserEventDescriptor eventDescriptor, string eventArgsJson)
        {
            var eventArgs = ParseEventArgsJson(eventDescriptor.EventArgsType, eventArgsJson);
            var browserRenderer = BrowserRendererRegistry.CurrentUserInstance.Find(eventDescriptor.BrowserRendererId);
            browserRenderer.DispatchBrowserEvent(
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
