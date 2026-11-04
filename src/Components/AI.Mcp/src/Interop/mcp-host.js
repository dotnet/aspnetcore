// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AppBridge } from "@modelcontextprotocol/ext-apps/app-bridge";
import { ProtocolError } from "@modelcontextprotocol/client";
import { bounded, checkSize, createTransport } from "./mcp-transport.js";

const sessions = new WeakMap();

export async function attach(frame, dotnet, proxyUrl, configuration, document, options) {
    const proxy = new URL(proxyUrl, location.href);
    if (proxy.origin === location.origin || !/^https?:$/.test(proxy.protocol)) {
        throw new Error("The MCP app proxy must use a distinct HTTP origin.");
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
        containerDimensions: { width: frame.clientWidth, maxHeight: configuration.maximumHeight }
    };
    const bridge = new AppBridge(null, { name: "Blazor MCP Apps host", version: "0.1.0" },
        { serverTools: {}, serverResources: {} }, { hostContext: context });
    const transport = createTransport(frame.contentWindow, proxy.origin, options.maximumMessageBytes);
    bridge.onerror = error => console.error("MCP Apps bridge failed.", error);
    const broker = async (method, parameters, signal) => {
        if (lifetime.signal.aborted) throw new Error("The MCP app is closed.");
        if (requests.size >= options.maximumPendingRequests) throw new Error("Too many MCP app requests.");
        const json = JSON.stringify({ method, parameters: parameters ?? {} });
        checkSize(json, options.maximumMessageBytes);
        const id = `${++nextId}`;
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
            const result = await bounded(dotnet.invokeMethodAsync("DispatchAsync", id, json),
                cancellation.signal, options.operationTimeout);
            checkSize(result, options.maximumMessageBytes);
            const response = JSON.parse(result);
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
    bridge.onsizechange = ({ height }) => {
        if (Number.isFinite(height)) frame.style.height = `${Math.max(1, Math.min(configuration.maximumHeight, height))}px`;
    };
    let signalSandbox;
    const sandboxReady = new Promise(resolve => { signalSandbox = resolve; });
    bridge.onsandboxready = () => signalSandbox();
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
                await bounded(Promise.allSettled(cancellations), new AbortController().signal, options.teardownTimeout);
                if (ready) await bridge.teardownResource({}, { timeout: Math.min(3000, options.teardownTimeout) });
            } finally {
                await bridge.close();
                frame.removeAttribute("src");
                sessions.delete(frame);
            }
        });
    }
    sessions.set(frame, close);
    try {
        await bridge.connect(transport);
        frame.src = proxy.href;
        await bounded(sandboxReady, lifetime.signal, options.initializationTimeout);
        const policy = document.policy;
        const permissions = Object.fromEntries((policy.permissions ?? []).map(feature =>
            [feature === "clipboard-write" ? "clipboardWrite" : feature, {}]));
        await bridge.sendSandboxResourceReady({
            html: document.html,
            csp: {
                resourceDomains: policy.resourceDomains,
                connectDomains: policy.connectDomains,
                frameDomains: policy.frameDomains,
                baseUriDomains: policy.baseUriDomains
            },
            permissions
        });
        await bounded(initialized, lifetime.signal, options.initializationTimeout);
        ready = true;
        await bridge.sendToolInput({ arguments: configuration.arguments ?? {} });
        resize = new ResizeObserver(() => {
            if (lifetime.signal.aborted || context.containerDimensions.width === frame.clientWidth) return;
            context = { ...context, containerDimensions: { width: frame.clientWidth, maxHeight: configuration.maximumHeight } };
            bridge.setHostContext(context);
        });
        resize.observe(frame);
        broker("$initialResult", {}, lifetime.signal).then(result => {
            if (!lifetime.signal.aborted) return bridge.sendToolResult(result);
        }, error => {
            if (!lifetime.signal.aborted) return bridge.sendToolCancelled({ reason: error.message });
        }).catch(error => {
            if (!lifetime.signal.aborted) bridge.onerror?.(error);
        });
        return {
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
