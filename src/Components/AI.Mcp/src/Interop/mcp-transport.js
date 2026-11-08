// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { JSONRPCMessageSchema } from "@modelcontextprotocol/core";

export function withCancellation(promise, signal) {
    return new Promise((resolve, reject) => {
        let settled = false;
        const complete = (callback, value) => {
            if (settled) return;
            settled = true;
            signal.removeEventListener("abort", abort);
            callback(value);
        };
        const abort = () => complete(reject, signal.reason ?? new Error("The MCP app closed."));
        signal.addEventListener("abort", abort, { once: true });
        if (signal.aborted) abort();
        Promise.resolve(promise).then(value => complete(resolve, value), error => complete(reject, error));
    });
}

// The SDK transport does not expose origin filtering.
export function createTransport(target, origin) {
    let active = false;
    const transport = {
        onmessage: undefined,
        onerror: error => console.error("MCP app transport failed.", error),
        onclose: undefined,
        async start() {
            if (active) throw new Error("The MCP app transport is already started.");
            active = true;
            window.addEventListener("message", receive);
        },
        async send(message) {
            if (!active) throw new Error("The MCP app transport is closed.");
            target.postMessage(message, origin);
        },
        async close() {
            if (!active) return;
            active = false;
            window.removeEventListener("message", receive);
            transport.onclose?.();
        }
    };
    function receive(event) {
        if (!active || event.source !== target || event.origin !== origin) return;
        try {
            const parsed = JSONRPCMessageSchema.parse(event.data);
            transport.onmessage?.(parsed);
        } catch (error) {
            transport.onerror?.(error);
        }
    }
    return transport;
}
