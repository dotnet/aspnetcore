// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure
{
    /// <summary>
    /// Contains methods called by interop. Intended for framework use only, not supported for use in application
    /// code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class JSInteropMethods
    {
        private static WebEventJsonContext? _jsonContext;

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uri, bool isInterceptedLink)
        {
            WebAssemblyNavigationManager.Instance.SetLocation(uri, isInterceptedLink);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(DispatchEvent))]
        public static Task DispatchEvent(WebEventDescriptor eventDescriptor, string eventArgsJson)
        {
            var renderer = RendererRegistry.Find(eventDescriptor.BrowserRendererId);

            // JsonSerializerOptions are tightly bound to the JsonContext. Cache it on first use using a copy
            // of the serializer settings.
            if (_jsonContext is null)
            {
                var jsonSerializerOptions = DefaultWebAssemblyJSRuntime.Instance.ReadJsonSerializerOptions();
                _jsonContext = new(new JsonSerializerOptions(jsonSerializerOptions));
            }

            var webEvent = WebEventData.Parse(renderer, _jsonContext, eventDescriptor, eventArgsJson);
            return renderer.DispatchEventAsync(
                webEvent.EventHandlerId,
                webEvent.EventFieldInfo,
                webEvent.EventArgs);
        }
    }
}
