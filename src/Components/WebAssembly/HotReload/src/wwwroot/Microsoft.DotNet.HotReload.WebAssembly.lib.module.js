export async function onRuntimeConfigLoaded(config) {
    // Disable HotReload built-into the Blazor WebAssembly runtime
    config.environmentVariables["__BLAZOR_WEBASSEMBLY_LEGACY_HOTRELOAD"] = "false";
}

export async function onRuntimeReady({ getAssemblyExports }) {
    const exports = await getAssemblyExports("Microsoft.DotNet.HotReload.WebAssembly");
    await exports.Microsoft.DotNet.HotReload.WebAssembly.Interop.InitializeAsync(document.baseURI);

    if (!window.Blazor) {
        window.Blazor = {};
    }

    window.Blazor._internal.applyHotReloadDeltas = (deltas, loggingLevel) => {
        const result = exports.Microsoft.DotNet.HotReload.WebAssembly.Interop.ApplyHotReloadDeltas(JSON.stringify(deltas), loggingLevel);
        return result ? JSON.parse(result) : [];
    };

    window.Blazor._internal.getApplyUpdateCapabilities = () => {
        return exports.Microsoft.DotNet.HotReload.WebAssembly.Interop.GetApplyUpdateCapabilities() ?? '';
    };
}
