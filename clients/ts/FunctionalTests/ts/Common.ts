// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpTransportType, IHubProtocol, JsonHubProtocol } from "@aspnet/signalr";
import { MessagePackHubProtocol } from "@aspnet/signalr-protocol-msgpack";

export const ECHOENDPOINT_URL = "http://" + document.location.host + "/echo";

export function getHttpTransportTypes(): HttpTransportType[] {
    const transportTypes = [];
    if (typeof WebSocket !== "undefined") {
        transportTypes.push(HttpTransportType.WebSockets);
    }
    if (typeof EventSource !== "undefined") {
        transportTypes.push(HttpTransportType.ServerSentEvents);
    }
    transportTypes.push(HttpTransportType.LongPolling);

    return transportTypes;
}

export function eachTransport(action: (transport: HttpTransportType) => void) {
    getHttpTransportTypes().forEach((t) => {
        return action(t);
    });
}

export function eachTransportAndProtocol(action: (transport: HttpTransportType, protocol: IHubProtocol) => void) {
    const protocols: IHubProtocol[] = [new JsonHubProtocol()];
    // IE9 does not support XmlHttpRequest advanced features so disable for now
    // This can be enabled if we fix: https://github.com/aspnet/SignalR/issues/742
    if (typeof new XMLHttpRequest().responseType === "string") {
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
