// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Worker client for communicating with the WebWorker.
 * This module is imported by the Blazor app to send requests to the worker.
 */

const pendingRequests = {};
let requestId = 0;
let workerError = null;
let worker = null;
let workerReady = false;
let workerReadyPromise = null;
let workerReadyResolve = null;
let workerReadyReject = null;
let progressCallback = null;

/**
 * Sets the progress callback function that will be called when the worker reports progress.
 * @param {function(string, number, number)} callback - Callback function(message, current, total)
 */
export function setProgressCallback(callback) {
    progressCallback = callback;
}

/**
 * Creates and initializes the WebWorker with event handlers.
 */
function createWorker() {
    worker = new Worker('_content/WebWorkerTemplate.WorkerClient/worker.js', { type: "module" });
    workerError = null;
    workerReady = false;
    
    // Create a promise that resolves when worker is ready
    workerReadyPromise = new Promise((resolve, reject) => {
        workerReadyResolve = resolve;
        workerReadyReject = reject;
    });

    // Handle fatal worker errors (script load failure, unhandled exceptions, etc.)
    worker.addEventListener('error', function (e) {
        const errorMessage = e.message || 'Worker encountered an unhandled error';
        console.error("Worker error:", errorMessage);
        workerError = errorMessage;
        
        // Reject ready promise if not yet resolved
        if (workerReadyReject) {
            workerReadyReject(new Error(errorMessage));
            workerReadyResolve = null;
            workerReadyReject = null;
        }
        
        // Reject all pending requests
        rejectAllPending(`Worker error: ${errorMessage}`);
    }, false);

    worker.addEventListener('message', function (e) {
        if (e.data.type === "ready") {
            if (e.data.error) {
                workerError = e.data.error;
                if (workerReadyReject) {
                    workerReadyReject(new Error(e.data.error));
                }
            } else {
                workerReady = true;
                if (workerReadyResolve) {
                    workerReadyResolve();
                }
            }
            workerReadyResolve = null;
            workerReadyReject = null;
        } else if (e.data.type === "progress") {
            // Handle progress updates
            if (progressCallback) {
                progressCallback(e.data.message, e.data.current, e.data.total);
            }
        } else if (e.data.type === "result") {
            const request = pendingRequests[e.data.requestId];
            if (!request) {
                // Result arrived after timeout/cancellation - ignore
                return;
            }
            delete pendingRequests[e.data.requestId];
            
            if (e.data.error) {
                request.reject(new Error(e.data.error));
            } else {
                request.resolve(e.data.result);
            }
        }
    }, false);
}

/**
 * Rejects all pending requests with the given error message.
 */
function rejectAllPending(errorMessage) {
    for (const id in pendingRequests) {
        pendingRequests[id].reject(new Error(errorMessage));
        delete pendingRequests[id];
    }
}

// Create the initial worker
createWorker();

/**
 * Invoke a method on the worker.
 * @param {string} method - Full method path: "Namespace.ClassName.MethodName"
 * @param {any[]} args - Arguments to pass to the method
 * @returns {Promise<any>} The result from the worker
 */
export async function invoke(method, args) {
    // If worker already failed, reject immediately
    if (workerError) {
        return Promise.reject(new Error(`Worker failed to initialize: ${workerError}`));
    }
    
    // Wait for worker to be ready before sending message
    if (!workerReady) {
        await workerReadyPromise;
    }
    
    requestId++;
    const currentRequestId = requestId;
    
    const promise = new Promise((resolve, reject) => {
        pendingRequests[currentRequestId] = { resolve, reject };
    });
    
    worker.postMessage({
        method: method,
        args: args,
        requestId: currentRequestId
    });
    
    return promise;
}

/**
 * Invoke a method on the worker that returns a string.
 * This is a convenience wrapper that ensures the result is returned as a string.
 * @param {string} method - Full method path: "Namespace.ClassName.MethodName"
 * @param {any[]} args - Arguments to pass to the method
 * @returns {Promise<string>} The string result from the worker
 */
export function invokeString(method, args) {
    return invoke(method, args).then(result => {
        // Ensure we return a string
        if (typeof result === 'string') {
            return result;
        }
        // If it's not already a string, convert it
        return String(result);
    });
}

/**
 * Terminates the current worker and creates a new one.
 * All pending requests will be rejected with an error.
 * Use this to recover from a stuck or unresponsive worker.
 */
export function terminate() {
    // Reject all pending requests
    rejectAllPending("Worker was terminated");
    
    // Kill the worker
    if (worker) {
        worker.terminate();
    }
    
    // Create a fresh worker
    createWorker();
}

/**
 * Wait for the worker to be fully initialized and ready.
 * @returns {Promise<void>} Resolves when worker is ready, rejects if initialization failed
 */
export function waitForReady() {
    if (workerReady) {
        return Promise.resolve();
    }
    if (workerError) {
        return Promise.reject(new Error(workerError));
    }
    return workerReadyPromise;
}
