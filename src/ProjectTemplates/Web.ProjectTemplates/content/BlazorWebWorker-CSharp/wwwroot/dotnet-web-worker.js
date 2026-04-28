let workerExports = {};
let startupError = null;

async function initialize(dotnetJsUrl, assemblyName) {
    try {
        const { dotnet } = await import(dotnetJsUrl);
        const { getAssemblyExports, getConfig } = await dotnet.create();
        const mainAssemblyName = getConfig().mainAssemblyName;

        workerExports = { ...await getAssemblyExports(mainAssemblyName) };

        if (assemblyName && assemblyName !== mainAssemblyName) {
            workerExports = { ...workerExports, ...await getAssemblyExports(assemblyName) };
        }

        self.postMessage({ type: "ready" });
    } catch (err) {
        const errorMessage = err?.message ?? String(err);
        startupError = errorMessage;
        console.error("[Worker] Failed to initialize .NET:", err);
        self.postMessage({ type: "ready", error: errorMessage });
    }
}

self.addEventListener('message', async (e) => {
    if (e.data.type === 'init') {
        await initialize(e.data.dotnetJsUrl, e.data.assemblyName);
        return;
    }

    const { method, args, requestId } = e.data;

    try {
        if (Object.keys(workerExports).length === 0) {
            throw new Error(startupError || "Worker .NET runtime not loaded");
        }

        const fn = method.split('.').reduce((obj, part) => obj?.[part], workerExports);
        if (typeof fn !== 'function') {
            throw new Error(`Method not found: ${method}`);
        }

        const result = await fn(...args);
        self.postMessage({ type: "result", requestId, result }, collectTransferables(result));
    } catch (err) {
        self.postMessage({ type: "result", requestId, error: err?.message ?? String(err) });
    }
});

function collectTransferables(value) {
    if (ArrayBuffer.isView(value)) return [value.buffer];
    if (value instanceof ArrayBuffer) return [value];
    return [];
}
