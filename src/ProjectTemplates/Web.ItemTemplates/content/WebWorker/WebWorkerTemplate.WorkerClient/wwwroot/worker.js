// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from '../../_framework/dotnet.js'

let workerExports = null;
let startupError;

try {
    const { getAssemblyExports, getConfig } = await dotnet.create();
    const config = getConfig();
    workerExports = await getAssemblyExports(config.mainAssemblyName);
    self.postMessage({ type: "ready" });
} catch (err) {
    startupError = err.message;
    console.error("[Worker] Failed to initialize .NET in worker:", err);
    self.postMessage({ type: "ready", error: err.message });
}

self.addEventListener('message', async (e) => {
    try {
        if (!workerExports) {
            throw new Error(startupError || "Worker .NET runtime not loaded");
        }

        const method = e.data.method.split('.').reduce((obj, part) => obj?.[part], workerExports);
        if (typeof method !== 'function') {
            throw new Error(`Method not found: ${e.data.method}`);
        }

        const result = await method(...e.data.args);
        const transferables = collectTransferables(result);

        self.postMessage({
            type: "result",
            requestId: e.data.requestId,
            result,
        }, transferables);
    } catch (err) {
        const errorMessage = err?.message ?? String(err);
        console.error('[Worker] Error:', errorMessage);
        self.postMessage({
            type: "result",
            requestId: e.data.requestId,
            error: errorMessage,
        });
    }
});

function collectTransferables(value) {
    if (ArrayBuffer.isView(value)) return [value.buffer];
    if (value instanceof ArrayBuffer) return [value];
    return [];
}
