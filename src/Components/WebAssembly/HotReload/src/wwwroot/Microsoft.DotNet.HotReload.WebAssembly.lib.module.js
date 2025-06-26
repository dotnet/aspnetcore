export async function onRuntimeReady({ getAssemblyExports }) {
    const exports = await getAssemblyExports("Microsoft.DotNet.HotReload.WebAssembly");
    await exports.Microsoft.AspNetCore.Components.WebAssembly.HotReload.Interop.InitializeAsync(document.baseURI);
}
