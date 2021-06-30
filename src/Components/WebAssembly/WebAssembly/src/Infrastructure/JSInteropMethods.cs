// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        /// <summary>
        /// Invoked via Mono's JS interop mechanism (invoke_method)
        ///
        /// Notifies .NET of an JS Stream Data Chunk that's available for transfer from JS to .NET
        ///
        /// Ideally that byte array would be transferred directly as a parameter on this
        /// call, however that's not currently possible due to: https://github.com/dotnet/runtime/issues/53378
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="chunkId"></param>
        /// <param name="error"></param>
        [JSInvokable("NotifyJSStreamDataChunkAvailable")]
        public static async Task NotifyJSStreamDataChunkAvailable(long streamId, long chunkId, string error)
        {
            var data = Array.Empty<byte>();
            if (string.IsNullOrEmpty(error))
            {
                data = DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<byte[]>("Blazor._internal.retrieveByteArray");
            }

            await WebAssemblyJSDataStream.ReceiveData(DefaultWebAssemblyJSRuntime.Instance, streamId, chunkId, data, error);
        }
    }
}
