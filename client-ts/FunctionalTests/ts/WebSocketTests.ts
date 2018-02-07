// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ECHOENDPOINT_URL } from "./Common";

if (typeof WebSocket !== "undefined") {
    describe("WebSockets", () => {
        it("can be used to connect to SignalR", (done) => {
            const message = "message";

            const webSocket = new WebSocket(ECHOENDPOINT_URL.replace(/^http/, "ws"));

            webSocket.onopen = () => {
                webSocket.send(message);
            };

            let received = "";
            webSocket.onmessage = (event) => {
                received += event.data;
                if (received === message) {
                    webSocket.close();
                }
            };

            webSocket.onclose = (event) => {
                if (!event.wasClean) {
                    fail("connection closed with unexpected status code: " + event.code + " " + event.reason);
                }

                // Jasmine doesn't like tests without expectations
                expect(event.wasClean).toBe(true);

                done();
            };
        });
    });
}
