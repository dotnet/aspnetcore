// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const references = new Map();
const retainedSignals = new Map();
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
            references.set(id, { controller });
            document.getElementById(`${id}-js`).textContent = "pending";
            try {
                document.getElementById(`${id}-js`).textContent = await target.invokeMethodAsync("Receive", signal, id, behavior);
            } catch (error) {
                document.getElementById(`${id}-js`).textContent = error.message;
            }
        },
        abort(id) {
            references.get(id).controller.abort();
        },
        async reuse(id) {
            const canceled = await target.invokeMethodAsync("Inspect", references.get(id).controller.signal);
            document.getElementById(`${id}-js`).textContent = `reused: ${canceled}`;
        },
        drop(id) {
            references.delete(id);
        },
        dropOutgoing() {
            retainedSignals.clear();
        },
        count() {
            return target.invokeMethodAsync("AutomaticProjectionCount");
        },
        async staticInvocation(preCanceled) {
            const controller = new AbortController();
            if (preCanceled) {
                controller.abort();
            }
            document.getElementById("static-js").textContent = await DotNet.invokeMethodAsync(
                "BasicTestApp", "StaticYolo", controller.signal, "static");
            controller.abort();
        },
        async snapshot() {
            const controller = new AbortController();
            const bytes = new Uint8Array([1, 2, 3]);
            let getters = 0;
            let toJson = 0;
            const input = {
                token: controller.signal,
                get bytes() { getters++; return bytes; },
                value: { toJSON() { toJson++; return "snapshot"; } },
                metadata: { __abortSignalArgument: 12 }
            };
            const pending = target.invokeMethodAsync("Snapshot", input);
            bytes[0] = 99;
            const result = await pending;
            document.getElementById("snapshot-js").textContent = `${result}:${getters}:${toJson}`;
            controller.abort();
        },
        async concurrent() {
            const controller = new AbortController();
            const results = await Promise.all([
                target.invokeMethodAsync("Inspect", controller.signal),
                target.invokeMethodAsync("Inspect", controller.signal)
            ]);
            const same = await target.invokeMethodAsync("SameTokens", controller.signal, controller.signal);
            const count = await target.invokeMethodAsync("AutomaticProjectionCount");
            document.getElementById("concurrent-js").textContent = `${results[0]}:${results[1]}:${same}:${count}`;
            controller.abort();
        },
        async wrapped() {
            const controller = new AbortController();
            const reference = DotNet.createJSObjectReference(controller.signal);
            try {
                const result = await target.invokeMethodAsync("ReceiveReference", reference);
                document.getElementById("wrapped-js").textContent = String(result);
            } finally {
                DotNet.disposeJSObjectReference(reference);
            }
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
    retainedSignals.set(id, signal);
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
    retainedSignals.set(id, signal);
    document.getElementById(`${id}-js`).textContent = "JS running with uncanceled signal";
    signal.addEventListener("abort", () => document.getElementById(`${id}-js`).textContent = "unexpected abort", { once: true });
    await new Promise(resolve => releaseControl = resolve);
    document.getElementById(`${id}-js`).textContent = signal.aborted ? "completed with aborted signal" : "JS completed normally";
    return "JS result";
}

export function disposeAll() {
    references.clear();
    retainedSignals.clear();
}
