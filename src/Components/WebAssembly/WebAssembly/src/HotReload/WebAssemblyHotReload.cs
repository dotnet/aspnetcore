// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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
            _hotReloadAgent = new HotReloadAgent(m => Debug.WriteLine(m));

            // Attempt to read previously applied hot reload deltas. dotnet-watch and VS will serve the script that can provide results from local-storage and
            // the injected middleware if present.
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
            var method = typeof(System.Reflection.Metadata.MetadataUpdater).GetMethod("GetCapabilities", BindingFlags.NonPublic | BindingFlags.Static, Type.EmptyTypes);
            if (method is null)
            {
                return string.Empty;
            }
            return (string)method.Invoke(obj: null, parameters: null)!;
        }
    }
}
