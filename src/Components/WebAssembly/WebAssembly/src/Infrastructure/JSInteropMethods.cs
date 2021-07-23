// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static async Task DispatchEvent(byte[] eventInfo)
        {
            var payload = GetJsonElements(eventInfo);
            var eventDescriptor = WebEventDescriptorReader.Read(payload.EventDescriptor);
            var renderer = RendererRegistry.Find(eventDescriptor.BrowserRendererId);

            var webEvent = WebEventData.Parse(
                renderer,
                DefaultWebAssemblyJSRuntime.Instance.ReadJsonSerializerOptions(),
                eventDescriptor,
                payload.EventArgs);

            await renderer.DispatchEventAsync(
                webEvent.EventHandlerId,
                webEvent.EventFieldInfo,
                webEvent.EventArgs);
        }

        private static (JsonElement EventDescriptor, JsonElement EventArgs) GetJsonElements(byte[] eventInfo)
        {
            // We receive data in the shape [eventDescriptor, eventArgs]
            var jsonReader = new Utf8JsonReader(eventInfo);
            var arrayElement = JsonElement.ParseValue(ref jsonReader);
            Debug.Assert(arrayElement.GetArrayLength() == 2, "Array length should be 2");
            return (arrayElement[0], arrayElement[1]);
        }
    }
}
