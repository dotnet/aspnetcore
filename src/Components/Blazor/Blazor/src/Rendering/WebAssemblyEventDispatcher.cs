// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    /// <summary>
    /// Dispatches events from JavaScript to a Blazor WebAssembly renderer.
    /// Intended for internal use only.
    /// </summary>
    public static class WebAssemblyEventDispatcher
    {
        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(DispatchEvent))]
        public static Task DispatchEvent(WebEventDescriptor eventDescriptor, string eventArgsJson)
        {
            var webEvent = WebEventData.Parse(eventDescriptor, eventArgsJson);
            var renderer = RendererRegistry.Find(eventDescriptor.BrowserRendererId);
            return renderer.DispatchEventAsync(
                webEvent.EventHandlerId,
                webEvent.EventFieldInfo,
                webEvent.EventArgs);
        }
    }
}
