export async function onRuntimeReady({ getAssemblyExports }) {
    const exports = await getAssemblyExports("Microsoft.DotNet.HotReload.WebAssembly");
    await exports.Microsoft.DotNet.HotReload.WebAssembly.Interop.InitializeAsync(document.baseURI);
}
