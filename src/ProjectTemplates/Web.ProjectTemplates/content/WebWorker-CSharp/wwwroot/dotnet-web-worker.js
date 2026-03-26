// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let workerExports = null;
let startupError = null;

async function initialize(dotnetJsUrl) {
    try {
        const { dotnet } = await import(dotnetJsUrl);
        const { getAssemblyExports, getConfig } = await dotnet.create();
        const assemblyName = getConfig().mainAssemblyName;
        workerExports = await getAssemblyExports(assemblyName);
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
        await initialize(e.data.dotnetJsUrl);
        return;
    }

    const { method, args, requestId } = e.data;

    try {
        if (!workerExports) {
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
