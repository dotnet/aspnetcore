'use strict';

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
