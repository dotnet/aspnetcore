// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { McpUiSandboxResourceReadyNotificationSchema } from "@modelcontextprotocol/ext-apps";
import { createTransport } from "./mcp-transport.js";

function origins(values, protocols = /^https?:$/) {
    return values.map(value => {
        const url = new URL(value);
        if (!protocols.test(url.protocol) || url.origin !== value) {
            throw new Error("MCP browser grants must be exact origins with an allowed scheme.");
        }
        return value;
    }).join(" ");
}

function loadDocument(html, csp, permissions) {
    const resources = origins(csp.resourceDomains ?? []);
    const connections = origins(csp.connectDomains ?? [], /^(https?|wss?):$/);
    const frames = origins(csp.frameDomains ?? []);
    const bases = origins(csp.baseUriDomains ?? []);
    const policy = `default-src 'none'; script-src 'unsafe-inline' ${resources}; style-src 'unsafe-inline' ${resources}; img-src data: ${resources}; font-src ${resources}; media-src ${resources}; connect-src ${connections || "'none'"}; frame-src ${frames || "'none'"}; base-uri ${bases || "'none'"}; form-action 'none'`;
    const parsed = new DOMParser().parseFromString(html, "text/html");
    const meta = parsed.createElement("meta");
    meta.httpEquiv = "Content-Security-Policy";
    meta.content = policy;
    parsed.head.prepend(meta);
    const frame = document.createElement("iframe");
    frame.title = "MCP app document";
    frame.setAttribute("sandbox", "allow-scripts");
    frame.referrerPolicy = "no-referrer";
    frame.allow = Object.keys(permissions).map(feature => {
        const permission = feature === "clipboardWrite" ? "clipboard-write" : feature;
        if (!/^[a-z][a-z-]*$/.test(permission)) throw new Error("Invalid browser feature grant.");
        return `${permission} *`;
    }).join("; ");
    frame.srcdoc = "<!doctype html>" + parsed.documentElement.outerHTML;
    document.body.append(frame);
    return frame;
}

export async function start(hostOrigin, options) {
    if (window.parent === window || new URL(hostOrigin).origin !== hostOrigin || hostOrigin === location.origin) {
        throw new Error("Invalid MCP app proxy parent origin.");
    }
    const transport = createTransport(parent, hostOrigin, options.maximumMessageBytes);
    let inner;
    transport.onmessage = message => {
        if (message.method === "ui/notifications/sandbox-resource-ready") {
            if (inner) throw new Error("The MCP app document is already attached.");
            const { html, csp = {}, permissions = {} } = McpUiSandboxResourceReadyNotificationSchema.parse(message).params;
            inner = loadDocument(html, csp, permissions);
        } else if (inner && !message.method?.startsWith("ui/notifications/sandbox-")) {
            inner.contentWindow.postMessage(message, "*");
        } else {
            throw new Error("The MCP app document is not ready.");
        }
    };
    const receive = event => {
        if (event.source === inner?.contentWindow && event.origin === "null") {
            transport.send(event.data).catch(error => transport.onerror?.(error));
        }
    };
    window.addEventListener("message", receive);
    window.addEventListener("pagehide", () => {
        window.removeEventListener("message", receive);
        transport.close().catch(error => console.error("MCP proxy cleanup failed.", error));
    }, { once: true });
    await transport.start();
    await transport.send({ jsonrpc: "2.0", method: "ui/notifications/sandbox-proxy-ready", params: {} });
}
