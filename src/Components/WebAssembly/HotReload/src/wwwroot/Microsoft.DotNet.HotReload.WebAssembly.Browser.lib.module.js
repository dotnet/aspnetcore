export async function onRuntimeConfigLoaded(config) {
    // If we have 'aspnetcore-browser-refresh', configure mono runtime for HotReload.
    if (config.debugLevel !== 0 && globalThis.window?.document?.querySelector("script[src*='aspnetcore-browser-refresh']")) {
        if (!config.environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"]) {
            config.environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
        }
        if (!config.environmentVariables["__ASPNETCORE_BROWSER_TOOLS"]) {
            config.environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = "true";
        }
    }

    // Disable HotReload built-into the Blazor WebAssembly runtime
    config.environmentVariables["__BLAZOR_WEBASSEMBLY_LEGACY_HOTRELOAD"] = "false";
}

export async function onRuntimeReady({ getAssemblyExports }) {
    const exports = await getAssemblyExports("Microsoft.DotNet.HotReload.WebAssembly.Browser");
    await exports.Microsoft.DotNet.HotReload.WebAssembly.Browser.WebAssemblyHotReload.InitializeAsync(document.baseURI);

    if (!window.Blazor) {
        window.Blazor = {};
    }

    window.Blazor._internal.applyHotReloadDeltas = (deltas, loggingLevel) => {
        const result = exports.Microsoft.DotNet.HotReload.WebAssembly.Browser.WebAssemblyHotReload.ApplyHotReloadDeltas(JSON.stringify(deltas), loggingLevel);
        return result ? JSON.parse(result) : [];
    };

    window.Blazor._internal.getApplyUpdateCapabilities = () => {
        return exports.Microsoft.DotNet.HotReload.WebAssembly.Browser.WebAssemblyHotReload.GetApplyUpdateCapabilities() ?? '';
    };
}
