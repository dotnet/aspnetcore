// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from '../../_framework/dotnet.js'

let workerExports = null;
let startupError = null;

try {
    const { getAssemblyExports, getConfig } = await dotnet.create();
    workerExports = await getAssemblyExports(getConfig().mainAssemblyName);
    self.postMessage({ type: "ready" });
} catch (err) {
    startupError = err.message;
    console.error("[Worker] Failed to initialize .NET:", err);
    self.postMessage({ type: "ready", error: err.message });
}

self.addEventListener('message', async (e) => {
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
