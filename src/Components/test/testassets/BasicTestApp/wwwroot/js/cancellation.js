// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const references = new Map();
const retainedSignals = new Map();
const collectedSignals = new Map();
const pageFinalizer = new FinalizationRegistry(id => collectedSignals.get(id)?.());
const nextTask = () => new Promise(resolve => setTimeout(resolve, 0));
let releaseControl;

export function initialize(target) {
    window.cancellationTests = {
        async start(id, behavior, alreadyAborted = false, stoppingListener = null) {
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
            if (stoppingListener) {
                // Registered before the interop sees the signal, so it runs before any listener the interop adds.
                signal.addEventListener("abort", event => event.stopImmediatePropagation(), { capture: stoppingListener === "capture" });
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
        async composite() {
            const controller = new AbortController();
            references.set("composite", { controller });
            // Not kept: a signal made by AbortSignal.any() has no controller, so only listeners keep it alive.
            const signal = AbortSignal.any([controller.signal]);
            document.getElementById("composite-js").textContent = await target.invokeMethodAsync("Receive", signal, "composite", "retain");
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
        async returned() {
            // Not kept, so only the interop can keep the signal alive until the token is canceled.
            const signal = await target.invokeMethodAsync("GetReturnedToken");
            document.getElementById("returned-js").textContent = signal instanceof AbortSignal ? "native signal received" : "not a signal";
            signal.addEventListener("abort", () => document.getElementById("returned-js").textContent = "aborted in JS", { once: true });
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
        sync() {
            const controller = new AbortController();
            references.set("sync", { controller });
            const aborted = new AbortController();
            aborted.abort();
            const live = target.invokeMethod("ReceiveSync", controller.signal);
            const preAborted = target.invokeMethod("Inspect", aborted.signal);
            document.getElementById("sync-js").textContent = `${live}:${preAborted}`;
        },
        abortSignalResult() {
            signalResultController.abort();
        },
        async syncReturned() {
            const result = target.invokeMethod("GetCancelingResult");
            await nextTask();
            document.getElementById("sync-returned-js").textContent = `${result.value}:${result.token.aborted}`;
        },
        async overtaken() {
            if (!await target.invokeMethodAsync("HoldNextInboundActivity")) {
                document.getElementById("overtaken-js").textContent = "no inbound gate";
                return;
            }
            // The server holds the call; the abort message is sent after it but is not held.
            const started = this.start("overtaken", "wait");
            this.abort("overtaken");
            await nextTask(); // The abort message is deferred by a microtask, so it is sent before the release below.
            await target.invokeMethodAsync("ReleaseHeldInboundActivity");
            await started;
        },
        async collectedDuringCall(abort) {
            const id = abort ? "collected" : "dropped";
            if (!await target.invokeMethodAsync("HoldNextInboundActivity")) {
                document.getElementById(`${id}-js`).textContent = "no inbound gate";
                return;
            }
            let controller = new AbortController();
            const pending = abort
                ? target.invokeMethodAsync("Receive", controller.signal, id, "wait")
                : target.invokeMethodAsync("Inspect", controller.signal);
            // The interop's own cleanup callback, as callbacks of different registries are not ordered.
            const collected = window.finalizationRegistryObserver?.cleanupOf(controller.signal);
            if (!collected) {
                document.getElementById(`${id}-js`).textContent = "no interop registration observed";
                await target.invokeMethodAsync("ReleaseHeldInboundActivity");
                return;
            }
            if (abort) {
                controller.abort();
            }
            // Nothing keeps the signal after this, so the test can collect it while the server holds the call.
            controller = null;
            document.getElementById(`${id}-js`).textContent = "pending";
            await collected;
            document.getElementById(`${id}-gc`).textContent = "signal collected";
            await target.invokeMethodAsync("ReleaseHeldInboundActivity");
            try {
                document.getElementById(`${id}-js`).textContent = String(await pending);
            } catch (error) {
                document.getElementById(`${id}-js`).textContent = error.message;
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

export function receiveAndForget(id, signal) {
    collectedSignals.set(id, () => document.getElementById(`${id}-gc`).textContent = "signal collected");
    pageFinalizer.register(signal, id);
    return "returned normally";
}

export function noop() {
}

// Kept so that the page can abort the returned signal later.
let signalResultController;
export function createSignal() {
    signalResultController = new AbortController();
    return signalResultController.signal;
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
