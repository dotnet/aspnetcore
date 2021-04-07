// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.HotReload;
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
            var jsonSerializerOptions = DefaultWebAssemblyJSRuntime.Instance.ReadJsonSerializerOptions();
            var webEvent = WebEventData.Parse(renderer, jsonSerializerOptions, eventDescriptor, eventArgsJson);
            return renderer.DispatchEventAsync(
                webEvent.EventHandlerId,
                webEvent.EventFieldInfo,
                webEvent.EventArgs);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(ApplyHotReloadDelta))]
        public static void ApplyHotReloadDelta(string moduleId, byte[] metadataDelta, byte[] ilDeta)
        {
            var moduleIdGuid = Guid.Parse(moduleId);
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.Modules.FirstOrDefault() is Module m && m.ModuleVersionId == moduleIdGuid);

            if (assembly is not null)
            {
                System.Reflection.Metadata.AssemblyExtensions.ApplyUpdate(assembly, metadataDelta, ilDeta, ReadOnlySpan<byte>.Empty);
            }

            // Remove this once there's a runtime API to subscribe to.
            typeof(ComponentBase).Assembly.GetType("Microsoft.AspNetCore.Components.HotReload.HotReloadManager")!.GetMethod("DeltaApplied", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null);
        }
    }
}
