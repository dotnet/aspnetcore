// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    /// <summary>
    /// Provides mechanisms for dispatching events to components in a <see cref="BrowserRenderer"/>.
    /// This is marked 'internal' because it only gets invoked from JS code.
    /// </summary>
    internal static class BrowserRendererEventDispatcher
    {
        // We receive the information as JSON strings because of current interop limitations:
        // - Can't pass unboxed value types from JS to .NET (yet all the IDs are ints)
        // - Can't pass more than 4 args from JS to .NET
        // This can be simplified in the future when the Mono WASM runtime is enhanced.
        public static void DispatchEvent(string eventDescriptorJson, string eventArgsJson)
        {
            var eventDescriptor = Json.Deserialize<BrowserEventDescriptor>(eventDescriptorJson);
            var eventArgs = ParseEventArgsJson(eventDescriptor.EventArgsType, eventArgsJson);
            var browserRenderer = BrowserRendererRegistry.Find(eventDescriptor.BrowserRendererId);
            browserRenderer.DispatchBrowserEvent(
                eventDescriptor.ComponentId,
                eventDescriptor.RenderTreeNodeIndex,
                eventArgs);
        }

        private static UIEventArgs ParseEventArgsJson(string eventArgsType, string eventArgsJson)
        {
            switch (eventArgsType)
            {
                case "mouse":
                    return Json.Deserialize<UIMouseEventArgs>(eventArgsJson);
                case "keyboard":
                    return Json.Deserialize<UIKeyboardEventArgs>(eventArgsJson);
                default:
                    throw new ArgumentException($"Unsupported value '{eventArgsType}'.", nameof(eventArgsType));
            }
        }

        private class BrowserEventDescriptor
        {
            public int BrowserRendererId { get; set; }
            public int ComponentId { get; set; }
            public int RenderTreeNodeIndex { get; set; }
            public string EventArgsType { get; set; }
        }
    }
}
