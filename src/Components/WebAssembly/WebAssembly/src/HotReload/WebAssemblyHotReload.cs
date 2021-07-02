// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.HotReload;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.HotReload
{
    /// <summary>
    /// Contains methods called by interop. Intended for framework use only, not supported for use in application
    /// code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WebAssemblyHotReload
    {
        private static HotReloadAgent? _hotReloadAgent;
        private static readonly UpdateDelta[] _updateDeltas = new[]
        {
            new UpdateDelta(),
        };

        internal static async Task InitializeAsync()
        {
            // Determine if we're running under a hot reload environment (e.g. dotnet-watch).
            // It's insufficient to know it the app can be hot reloaded (HotReloadFeature.IsSupported),
            // since the hot-reload agent might be unavailable.
            if (Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES") != "debug")
            {
                return;
            }

            _hotReloadAgent = new HotReloadAgent(m => Debug.WriteLine(m));

            var jsObjectReference = (IJSUnmarshalledObjectReference)(await DefaultWebAssemblyJSRuntime.Instance.InvokeAsync<IJSObjectReference>("import", "./_framework/blazor-hotreload.js"));
            await jsObjectReference.InvokeUnmarshalled<Task<int>>("receiveHotReload");
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(ApplyHotReloadDelta))]
        public static void ApplyHotReloadDelta(string moduleIdString, byte[] metadataDelta, byte[] ilDeta)
        {
            var moduleId = Guid.Parse(moduleIdString);

            _updateDeltas[0].ModuleId = moduleId;
            _updateDeltas[0].MetadataDelta = metadataDelta;
            _updateDeltas[0].ILDelta = ilDeta;

            _hotReloadAgent!.ApplyDeltas(_updateDeltas);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(GetApplyUpdateCapabilities))]
        public static string GetApplyUpdateCapabilities()
        {
            var method = typeof(System.Reflection.Metadata.AssemblyExtensions).GetMethod("GetApplyUpdateCapabilities", BindingFlags.NonPublic | BindingFlags.Static);
            if (method is null)
            {
                return string.Empty;
            }
            return (string)method.Invoke(obj: null, parameters: null)!;
        }
    }
}
