// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ECHOENDPOINT_URL } from "./Common";
import "./LogBannerReporter";

describe("WebSockets", () => {
    it("can be used to connect to SignalR", (done) => {
        const message = "message";

        let webSocket: WebSocket;
        if (typeof window !== "undefined") {
            if (typeof WebSocket !== "undefined") {
                webSocket = new WebSocket(ECHOENDPOINT_URL.replace(/^http/, "ws"));
            } else {
                // Running in a browser that doesn't support WebSockets
                done();
                return;
            }
        } else {
            const websocketModule = require("ws");
            if (websocketModule) {
                webSocket = new websocketModule(ECHOENDPOINT_URL.replace(/^http/, "ws"));
            } else {
                // No WebSockets implementations in current environment, skip test
                done();
                return;
            }
        }

        webSocket.onopen = () => {
            webSocket.send(message);
        };

        webSocket.onmessage = (event) => {
            expect(event.data).toEqual(message);
            webSocket.close();
        };

        webSocket.onclose = (event) => {
            expect(event.code).toEqual(1000);
            expect(event.wasClean).toBe(true);

            done();
        };
    });
});
