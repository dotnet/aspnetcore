// Standalone WASM: called by dotnet.js when the mono runtime config is loaded.
// Fetches client configuration from the gateway's /_blazor/_configuration endpoint
// and injects the values as environment variables on the MonoConfig.
export async function onRuntimeConfigLoaded(config) {
    try {
        const configUrl = new URL('_blazor/_configuration', document.baseURI).href;
        const response = await fetch(configUrl);
        if (response.ok) {
            const serverConfig = await response.json();

            const envVars = serverConfig?.webAssembly?.environment;
            if (envVars && Object.keys(envVars).length > 0) {
                config.environmentVariables ??= {};

                for (const [key, value] of Object.entries(envVars)) {
                    config.environmentVariables[key] = value;
                }
            }
        }
    } catch (error) {
    }
}
