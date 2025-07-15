export async function onRuntimeConfigLoaded(config) {
    const value = "false";
    debugger;
    config.environmentVariables["__BLAZOR_WEBASSEMBLY_LEGACY_HOTRELOAD"] = value;
}

export async function onRuntimeReady({ getAssemblyExports }) {
    const exports = await getAssemblyExports("Microsoft.DotNet.HotReload.WebAssembly");
    await exports.Microsoft.DotNet.HotReload.WebAssembly.Interop.InitializeAsync(document.baseURI);

    if (!window.Blazor) {
        window.Blazor = {};
    }

    window.Blazor._internal.applyHotReloadDeltas = (deltas, loggingLevel) => {
        return DotNet.invokeMethod('Microsoft.DotNet.HotReload.WebAssembly', 'ApplyHotReloadDeltas', deltas, loggingLevel) ?? [];
    };

    window.Blazor._internal.getApplyUpdateCapabilities = () => {
        return exports.Microsoft.DotNet.HotReload.WebAssembly.Interop.GetApplyUpdateCapabilities() ?? '';
    };
}
