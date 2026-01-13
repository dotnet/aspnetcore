// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * WebWorker entry point for .NET code execution.
 * This worker loads .NET runtime and dynamically routes method calls.
 */

import { dotnet } from '../../_framework/dotnet.js'

/**
 * Sends a progress update to the main thread.
 * @param {string} message - Progress message
 * @param {number} current - Current progress value
 * @param {number} total - Total progress value
 */
export function postProgress(message, current, total) {
    self.postMessage({
        type: "progress",
        message: message,
        current: current,
        total: total
    });
}

// Make postProgress globally available for .NET JSImport
globalThis.postProgress = postProgress;

/**
 * Collects ArrayBuffer objects from a value for zero-copy transfer.
 * @param {any} value - Value that may be or contain typed arrays
 * @returns {ArrayBuffer[]} Array of transferable buffers
 */
function collectTransferables(value) {
    const transferables = [];
    if (value instanceof Uint8Array || value instanceof Int8Array ||
        value instanceof Uint16Array || value instanceof Int16Array ||
        value instanceof Uint32Array || value instanceof Int32Array ||
        value instanceof Float32Array || value instanceof Float64Array) {
        transferables.push(value.buffer);
    } else if (value instanceof ArrayBuffer) {
        transferables.push(value);
    }
    return transferables;
}

let workerExports = null;
let startupError = undefined;

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
self.addEventListener('message', async function (e) {
    try {
        if (!workerExports) {
            throw new Error(startupError || "Worker .NET runtime not loaded");
        }

        // Parse full path: "Namespace.SubNamespace.ClassName.MethodName"
        // Last part is method, second-to-last is class, rest is namespace
        const parts = e.data.method.split('.');
        if (parts.length < 3) {
            throw new Error(`Invalid method path: ${e.data.method}. Expected format: Namespace.ClassName.MethodName`);
        }
        
        const methodName = parts.pop();
        const className = parts.pop();
        const namespacePath = parts.join('.');
        
        // Navigate to the namespace (e.g., "MyApp.Worker" -> workerExports.MyApp.Worker)
        let namespaceObj = workerExports;
        for (const part of parts) {
            namespaceObj = namespaceObj?.[part];
        }
        
        if (!namespaceObj) {
            console.error('[Worker] Available namespaces:', Object.keys(workerExports || {}));
            throw new Error(`Namespace not found: ${namespacePath}. Make sure your worker class is in this namespace.`);
        }
        
        const workerClass = namespaceObj[className];
        
        if (!workerClass) {
            throw new Error(`Class not found: ${className} in namespace ${namespacePath}. Make sure the class has [JSExport] methods.`);
        }

        const method = workerClass[methodName];
        if (!method) {
            throw new Error(`Method not found: ${methodName} in class ${namespacePath}.${className}`);
        }

        // Call the method with provided arguments
        const startTime = performance.now();
        let result = method(...e.data.args);
        
        // If result is a Promise (async method), await it
        if (result && typeof result.then === 'function') {
            result = await result;
        }
        
        const workerTime = performance.now() - startTime;

        // Collect transferables from result for zero-copy transfer back
        const transferables = collectTransferables(result);
        
        self.postMessage({
            type: "result",
            requestId: e.data.requestId,
            result: result,
            workerTime: workerTime,
        }, transferables);
    } catch (err) {
        // Handle both Error objects and other thrown values
        const errorMessage = err instanceof Error ? err.message : String(err);
        console.error('[Worker] Error:', errorMessage);
        self.postMessage({
            type: "result",
            requestId: e.data.requestId,
            error: errorMessage,
        });
    }
}, false);
