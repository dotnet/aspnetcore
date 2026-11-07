// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AppBridge } from "@modelcontextprotocol/ext-apps/app-bridge";
import { ProtocolError } from "@modelcontextprotocol/client";
import { withCancellation, createTransport } from "./mcp-transport.js";

const sessions = new WeakMap();
const pendingReady = new WeakMap();
const readiness = new WeakMap();

window.addEventListener("message", event => {
    if (event.source && event.data?.jsonrpc === "2.0" &&
        !Object.hasOwn(event.data, "id") &&
        event.data.method === "ui/notifications/sandbox-proxy-ready" &&
        event.data.params && typeof event.data.params === "object" && !Array.isArray(event.data.params)) {
        const active = readiness.get(event.source);
        if (active) {
            if (event.origin === active.origin) active.signal();
        } else {
            pendingReady.set(event.source, event.origin);
        }
    }
});

export async function attach(frame, dotnet, configuration, document) {
    if (!(frame instanceof HTMLIFrameElement) || !frame.hasAttribute("src") || sessions.has(frame)) {
        throw new Error("The MCP host requires an unattached iframe with a sandbox URL.");
    }
    const proxy = new URL(frame.src, location.href);
    if (proxy.origin === location.origin) {
        throw new Error("The MCP app proxy must use a distinct origin.");
    }
    const lifetime = new AbortController();
    const requests = new Map();
    let nextId = 0;
    let context = {
        toolInfo: { tool: configuration.tool },
        theme: "light",
        displayMode: "inline",
        availableDisplayModes: ["inline"],
        platform: "web",
        locale: navigator.language,
        containerDimensions: { width: frame.clientWidth, height: frame.clientHeight }
    };
    const bridge = new AppBridge(null, { name: "Blazor MCP Apps host", version: "0.1.0" },
        { serverTools: {}, serverResources: {} }, { hostContext: context });
    const transport = createTransport(frame.contentWindow, proxy.origin);
    bridge.onerror = error => console.error("MCP Apps bridge failed.", error);
    const broker = async (method, parameters, signal) => {
        if (lifetime.signal.aborted) throw new Error("The MCP app is closed.");
        const id = `${++nextId}`;
        const json = JSON.stringify({ jsonrpc: "2.0", id, method, params: parameters ?? {} });
        const cancellation = new AbortController();
        const cancel = () => {
            cancellation.abort();
            const delivery = dotnet.invokeMethodAsync("Cancel", id);
            delivery.catch(error => console.error("MCP app cancellation delivery failed.", error));
            return delivery;
        };
        requests.set(id, cancel);
        signal?.addEventListener("abort", cancel, { once: true });
        lifetime.signal.addEventListener("abort", cancel, { once: true });
        try {
            if (signal?.aborted) throw signal.reason ?? new Error("The request was cancelled.");
            const result = await withCancellation(dotnet.invokeMethodAsync("DispatchAsync", json),
                cancellation.signal);
            const response = JSON.parse(result);
            if (response.jsonrpc !== "2.0" || response.id !== id) {
                throw new Error("The MCP app response does not match its request.");
            }
            if (response.error) throw new ProtocolError(response.error.code, response.error.message);
            return response.result;
        } finally {
            requests.delete(id);
            signal?.removeEventListener("abort", cancel);
            lifetime.signal.removeEventListener("abort", cancel);
        }
    };
    bridge.oncalltool = (parameters, extra) => broker("tools/call", parameters, extra.mcpReq.signal);
    bridge.onlisttools = (parameters, extra) => broker("tools/list", parameters, extra.mcpReq.signal);
    bridge.onreadresource = (parameters, extra) => broker("resources/read", parameters, extra.mcpReq.signal);
    bridge.onlistresources = (parameters, extra) => broker("resources/list", parameters, extra.mcpReq.signal);
    bridge.onrequestdisplaymode = async () => ({ mode: "inline" });
    bridge.onsizechange = () => {};
    let signalSandbox;
    const sandboxReady = new Promise(resolve => { signalSandbox = resolve; });
    bridge.onsandboxready = () => signalSandbox();
    const target = frame.contentWindow;
    readiness.set(target, { origin: proxy.origin, signal: signalSandbox });
    let signalInitialized;
    const initialized = new Promise(resolve => { signalInitialized = resolve; });
    bridge.oninitialized = () => signalInitialized();
    let ready = false;
    let closing;
    let resize;
    const observer = new MutationObserver(() => {
        if (!frame.isConnected) close().catch(error => console.error("MCP app cleanup failed.", error));
    });
    observer.observe(frame.ownerDocument, { childList: true, subtree: true });
    function close() {
        return closing ??= Promise.resolve().then(async () => {
            lifetime.abort(new Error("The MCP app closed."));
            observer.disconnect();
            resize?.disconnect();
            try {
                const cancellations = [...requests.values()].map(cancel => cancel());
                await Promise.allSettled(cancellations);
                if (ready) await bridge.teardownResource({});
            } finally {
                try {
                    await bridge.close();
                } finally {
                    pendingReady.delete(target);
                    readiness.delete(target);
                    sessions.delete(frame);
                }
            }
        });
    }
    sessions.set(frame, close);
    try {
        await bridge.connect(transport);
        const capturedOrigin = pendingReady.get(target);
        pendingReady.delete(target);
        if (capturedOrigin === proxy.origin) signalSandbox();
        await withCancellation(sandboxReady, lifetime.signal);
        await bridge.sendSandboxResourceReady({
            html: document.html,
            ...(document.csp === null ? {} : { csp: document.csp }),
            ...(document.permissions === null ? {} : { permissions: document.permissions })
        });
        await withCancellation(initialized, lifetime.signal);
        ready = true;
        await bridge.sendToolInput({ arguments: configuration.arguments ?? {} });
        resize = new ResizeObserver(() => {
            if (lifetime.signal.aborted || (context.containerDimensions.width === frame.clientWidth &&
                context.containerDimensions.height === frame.clientHeight)) return;
            context = { ...context, containerDimensions: { width: frame.clientWidth, height: frame.clientHeight } };
            bridge.setHostContext(context);
        });
        resize.observe(frame);
        return {
            async sendToolResult(result) {
                if (lifetime.signal.aborted) throw new Error("The MCP app is closed.");
                await bridge.sendToolResult(result);
            },
            async sendToolCancelled(reason) {
                if (lifetime.signal.aborted) throw new Error("The MCP app is closed.");
                await bridge.sendToolCancelled({ reason });
            },
            updateHostContext(update) {
                if (lifetime.signal.aborted) throw new Error("The MCP app is closed.");
                context = { ...context, ...update };
                bridge.setHostContext(context);
            }
        };
    } catch (error) {
        await close();
        throw error;
    }
}

export async function detach(frame) {
    await sessions.get(frame)?.();
}
