// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpClient, HttpTransportType, IHubProtocol, JsonHubProtocol } from "@microsoft/signalr";
import { MessagePackHubProtocol } from "@microsoft/signalr-protocol-msgpack";
import { TestLogger } from "./TestLogger";

import { FetchHttpClient } from "@microsoft/signalr/dist/esm/FetchHttpClient";
import { Platform } from "@microsoft/signalr/dist/esm/Utils";
import { XhrHttpClient } from "@microsoft/signalr/dist/esm/XhrHttpClient";

export const DEFAULT_TIMEOUT_INTERVAL: number = 40 * 1000;
export let ENDPOINT_BASE_URL: string = "";
export let ENDPOINT_BASE_HTTPS_URL: string = "";

// On slower CI machines, these tests sometimes take longer than 5s
jasmine.DEFAULT_TIMEOUT_INTERVAL = DEFAULT_TIMEOUT_INTERVAL;

if (typeof window !== "undefined" && (window as any).__karma__) {
    const args = (window as any).__karma__.config.args as string[];
    let httpsServer = "";
    let httpServer = "";
    let sauce = false;

    for (let i = 0; i < args.length; i += 1) {
        switch (args[i]) {
            case "--server": {
                i += 1;
                const urls = args[i].split(";");
                httpServer = urls[1];
                httpsServer = urls[0];
                break;
            }
            case "--sauce":
                sauce = true;
                break;
        }
    }

    // Increase test timeout in sauce because of the proxy
    if (sauce) {
        // Double the timeout.
        jasmine.DEFAULT_TIMEOUT_INTERVAL *= 2;
    }

    // Running in Karma? Need to use an absolute URL
    ENDPOINT_BASE_URL = httpServer;
    ENDPOINT_BASE_HTTPS_URL = httpsServer;
} else if (typeof document !== "undefined") {
    ENDPOINT_BASE_URL = `${document.location.protocol}//${document.location.host}`;
} else if (process && process.env && process.env.SERVER_URL) {
    const urls = process.env.SERVER_URL.split(";");
    ENDPOINT_BASE_URL = urls[1];
    ENDPOINT_BASE_HTTPS_URL = urls[0];
} else {
    throw new Error("The server could not be found.");
}

console.log(`Using SignalR HTTP Server: '${ENDPOINT_BASE_URL}'`);
console.log(`Using SignalR HTTPS Server: '${ENDPOINT_BASE_HTTPS_URL}'`);
console.log(`Jasmine DEFAULT_TIMEOUT_INTERVAL: ${jasmine.DEFAULT_TIMEOUT_INTERVAL}`);

export const ECHOENDPOINT_URL = ENDPOINT_BASE_URL + "/echo";
export const HTTPS_ECHOENDPOINT_URL = ENDPOINT_BASE_HTTPS_URL + "/echo";

export function getHttpTransportTypes(): HttpTransportType[] {
    const transportTypes = [];
    if (typeof window === "undefined") {
        transportTypes.push(HttpTransportType.WebSockets);
        transportTypes.push(HttpTransportType.ServerSentEvents);
    } else {
        if (typeof WebSocket !== "undefined") {
            transportTypes.push(HttpTransportType.WebSockets);
        }
        if (typeof EventSource !== "undefined") {
            transportTypes.push(HttpTransportType.ServerSentEvents);
        }
    }
    transportTypes.push(HttpTransportType.LongPolling);

    return transportTypes;
}

export function eachTransport(action: (transport: HttpTransportType) => void): void {
    getHttpTransportTypes().forEach((t) => {
        return action(t);
    });
}

export function eachTransportAndProtocol(action: (transport: HttpTransportType, protocol: IHubProtocol) => void): void {
    const protocols: IHubProtocol[] = [new JsonHubProtocol()];
    // Run messagepack tests in Node and Browsers that support binary content (indicated by the presence of responseType property)
    if (typeof XMLHttpRequest === "undefined" || typeof new XMLHttpRequest().responseType === "string") {
        // Because of TypeScript stuff, we can't get "ambient" or "global" declarations to work with the MessagePackHubProtocol module
        // This is only a limitation of the .d.ts file.
        // Everything works fine in the module
        protocols.push(new MessagePackHubProtocol());
    }
    getHttpTransportTypes().forEach((t) => {
        return protocols.forEach((p) => {
            if (t !== HttpTransportType.ServerSentEvents || !(p instanceof MessagePackHubProtocol)) {
                return action(t, p);
            }
        });
    });
}

export function eachTransportAndProtocolAndHttpClient(action: (transport: HttpTransportType, protocol: IHubProtocol, httpClient: HttpClient) => void): void {
    eachTransportAndProtocol((transport, protocol) => {
        getHttpClients().forEach((httpClient) => {
            action(transport, protocol, httpClient);
        });
    });
}

export function getGlobalObject(): any {
    return typeof window !== "undefined" ? window : global;
}

export function getHttpClients(): HttpClient[] {
    const httpClients: HttpClient[] = [];
    if (typeof XMLHttpRequest !== "undefined") {
        httpClients.push(new XhrHttpClient(TestLogger.instance));
    }
    if (typeof fetch !== "undefined" || Platform.isNode) {
        httpClients.push(new FetchHttpClient(TestLogger.instance));
    }
    return httpClients;
}

export function eachHttpClient(action: (transport: HttpClient) => void): void {
    return getHttpClients().forEach((t) => {
        return action(t);
    });
}

// Run test in Node or Chrome, but not on macOS
export const shouldRunHttpsTests =
    // Need to have an HTTPS URL
    !!ENDPOINT_BASE_HTTPS_URL &&

    // Run on Node, unless macOS
    (process && process.platform !== "darwin") &&

    // Only run under Chrome browser
    (typeof navigator === "undefined" || navigator.userAgent.search("Chrome") !== -1);
