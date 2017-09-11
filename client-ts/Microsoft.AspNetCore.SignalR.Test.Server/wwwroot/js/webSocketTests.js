// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

'use strict';

if (typeof WebSocket !== 'undefined') {
    describe('WebSockets', function () {
        it('can be used to connect to SignalR', function (done) {
            var message = "message";

            var webSocket = new WebSocket(ECHOENDPOINT_URL.replace(/^http/, "ws"));

            webSocket.onopen = function () {
                webSocket.send(message);
            };

            var received = "";
            webSocket.onmessage = function (event) {
                received += event.data;
                if (received === message) {
                    webSocket.close();
                }
            };

            webSocket.onclose = function (event) {
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
