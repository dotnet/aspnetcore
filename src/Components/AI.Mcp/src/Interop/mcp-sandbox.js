// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { McpUiSandboxResourceReadyNotificationSchema } from "@modelcontextprotocol/ext-apps";
import { buildAllowAttribute } from "@modelcontextprotocol/ext-apps/app-bridge";
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

function loadDocument(frame, html, csp, permissions) {
    const resources = origins(csp.resourceDomains ?? []);
    const connections = origins(csp.connectDomains ?? [], /^(https?|wss?):$/);
    const frames = origins(csp.frameDomains ?? []);
    const bases = origins(csp.baseUriDomains ?? []);
    const policy = `default-src 'none'; script-src 'unsafe-inline' ${resources}; style-src 'unsafe-inline' ${resources}; img-src data: ${resources}; font-src ${resources || "'none'"}; media-src ${resources || "'none'"}; connect-src ${connections || "'none'"}; frame-src ${frames || "'none'"}; base-uri ${bases || "'self'"}; form-action 'none'`;
    const parsed = new DOMParser().parseFromString(html, "text/html");
    const meta = parsed.createElement("meta");
    meta.httpEquiv = "Content-Security-Policy";
    meta.content = policy;
    parsed.head.prepend(meta);
    if (!frame.hasAttribute("allow")) frame.setAttribute("allow", buildAllowAttribute(permissions));
    frame.srcdoc = "<!doctype html>" + parsed.documentElement.outerHTML;
}

export async function start(frame, hostOrigin) {
    if (!(frame instanceof HTMLIFrameElement) || frame.id !== "mcp-app" ||
        !frame.isConnected || document.querySelectorAll("#mcp-app").length !== 1) {
        throw new Error("The MCP sandbox must render exactly one iframe with id=\"mcp-app\".");
    }
    const transport = createTransport(parent, hostOrigin);
    let loaded = false;
    transport.onmessage = message => {
        if (message.method === "ui/notifications/sandbox-resource-ready") {
            if (loaded) throw new Error("The MCP app document is already attached.");
            const { html, csp = {}, permissions = {} } = McpUiSandboxResourceReadyNotificationSchema.parse(message).params;
            loadDocument(frame, html, csp, permissions);
            loaded = true;
        } else if (loaded && !message.method?.startsWith("ui/notifications/sandbox-")) {
            frame.contentWindow.postMessage(message, "*");
        } else {
            throw new Error("The MCP app document is not ready.");
        }
    };
    const receive = event => {
        if (loaded && event.source === frame.contentWindow && event.origin === "null") {
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
