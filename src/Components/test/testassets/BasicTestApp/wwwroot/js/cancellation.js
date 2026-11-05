// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const references = new Map();
let releaseControl;

export function initialize(target) {
    window.cancellationTests = {
        async start(id, behavior, alreadyAborted = false) {
            const controller = new AbortController();
            const signal = controller.signal;
            let listeners = 0;
            const add = signal.addEventListener.bind(signal);
            const remove = signal.removeEventListener.bind(signal);
            signal.addEventListener = (...args) => {
                document.getElementById(`${id}-listeners`).textContent = ++listeners;
                add(...args);
            };
            signal.removeEventListener = (...args) => {
                document.getElementById(`${id}-listeners`).textContent = --listeners;
                remove(...args);
            };
            if (alreadyAborted) {
                controller.abort();
            }
            const reference = await target.createCancellationTokenReference(signal);
            references.set(id, { controller, reference });
            document.getElementById(`${id}-js`).textContent = "pending";
            try {
                document.getElementById(`${id}-js`).textContent = await target.invokeMethodAsync("Receive", reference, id, behavior);
            } catch (error) {
                document.getElementById(`${id}-js`).textContent = error.message;
            }
        },
        abort(id) {
            references.get(id).controller.abort();
        },
        async reuse(id) {
            const canceled = await target.invokeMethodAsync("Inspect", references.get(id).reference);
            document.getElementById(`${id}-js`).textContent = `reused: ${canceled}`;
        },
        async dispose(id) {
            await references.get(id).reference.disposeAsync();
            document.getElementById(`${id}-js`).textContent = "reference disposed";
        },
        releaseControl() {
            releaseControl();
        }
    };
}

export function receive(id, signal) {
    if (!(signal instanceof AbortSignal)) {
        throw new Error("Expected a native AbortSignal");
    }
    document.getElementById(`${id}-js`).textContent = signal.aborted ? "aborted on entry" : "native signal retained";
    signal.addEventListener("abort", () => document.getElementById(`${id}-js`).textContent = "aborted in JS", { once: true });
    if (signal.aborted) {
        fetch("data:text/plain,value", { signal }).then(
            () => document.getElementById(`${id}-listeners`).textContent = "fetch unexpectedly completed",
            error => document.getElementById(`${id}-listeners`).textContent = error.name);
    }
    return "returned normally";
}

export async function waitIgnoringSignal(id, signal) {
    if (!(signal instanceof AbortSignal)) {
        throw new Error("Expected a native AbortSignal");
    }
    document.getElementById(`${id}-js`).textContent = "JS running with uncanceled signal";
    signal.addEventListener("abort", () => document.getElementById(`${id}-js`).textContent = "unexpected abort", { once: true });
    await new Promise(resolve => releaseControl = resolve);
    document.getElementById(`${id}-js`).textContent = signal.aborted ? "completed with aborted signal" : "JS completed normally";
    return "JS result";
}

export async function disposeAll() {
    for (const { reference } of references.values()) {
        await reference.disposeAsync();
    }
    references.clear();
}
