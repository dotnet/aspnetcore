// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * WebWorker entry point for .NET code execution.
 * This worker loads .NET runtime and dynamically routes method calls.
 */

import { dotnet } from '../../_framework/dotnet.js'

/**
 * Collects ArrayBuffer objects from a value for zero-copy transfer.
 * @param {any} value - Value that may be or contain typed arrays
 * @returns {ArrayBuffer[]} Array of transferable buffers
 */
function collectTransferables(value) {
    if (ArrayBuffer.isView(value)) return [value.buffer];
    if (value instanceof ArrayBuffer) return [value];
    return [];
}

let workerExports = null;
let startupError;

// Initialize .NET runtime in the worker
try {
    const { getAssemblyExports, getConfig } = await dotnet.create();
    const config = getConfig();
    
    // Get exports from the main assembly (where user's worker code lives)
    workerExports = await getAssemblyExports(config.mainAssemblyName);
    
    // Signal that the worker is ready
    self.postMessage({ type: "ready" });
} catch (err) {
    startupError = err.message;
    console.error("[Worker] Failed to initialize .NET in worker:", err);
    // Signal that the worker failed to initialize
    self.postMessage({ type: "ready", error: err.message });
}

// Listen for method invocations from the client
self.addEventListener('message', async (e) => {
    try {
        if (!workerExports) {
            throw new Error(startupError || "Worker .NET runtime not loaded");
        }

        // Resolve method from path: "Namespace.ClassName.MethodName"
        const method = e.data.method.split('.').reduce((obj, part) => obj?.[part], workerExports);
        if (typeof method !== 'function') {
            throw new Error(`Method not found: ${e.data.method}`);
        }

        // Call the method with provided arguments
        const startTime = performance.now();
        const result = await method(...e.data.args);
        const workerTime = performance.now() - startTime;

        // Collect transferables from result for zero-copy transfer back
        const transferables = collectTransferables(result);
        
        self.postMessage({
            type: "result",
            requestId: e.data.requestId,
            result,
            workerTime,
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
