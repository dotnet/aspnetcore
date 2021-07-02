// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        public static async Task DispatchEvent(string eventDescriptorJson, string eventArgsJson)
        {
            var eventDescriptorElement = GetJsonElement(eventDescriptorJson);
            var eventDescriptor = WebEventDescriptorReader.Read(eventDescriptorElement);
            var renderer = RendererRegistry.Find(eventDescriptor.BrowserRendererId);

            var eventArgsElement = GetJsonElement(eventArgsJson);

            var webEvent = WebEventData.Parse(renderer, DefaultWebAssemblyJSRuntime.Instance.ReadJsonSerializerOptions(), eventDescriptor, eventArgsElement);
            await renderer.DispatchEventAsync(
                webEvent.EventHandlerId,
                webEvent.EventFieldInfo,
                webEvent.EventArgs);
        }

        private static JsonElement GetJsonElement(string jsonString)
        {
            var byteLength = Encoding.UTF8.GetByteCount(jsonString);
            var bytes = ArrayPool<byte>.Shared.Rent(byteLength);

            try
            {
                var writtenBytes = Encoding.UTF8.GetBytes(jsonString, bytes);
                Debug.Assert(writtenBytes == byteLength);

                var jsonReader = new Utf8JsonReader(bytes.AsSpan(0, writtenBytes));
                return JsonElement.ParseValue(ref jsonReader);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
    }
}
