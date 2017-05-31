describe('WebSockets', function () {
    it('can be used to connect to SignalR', done => {
        const message = "message";

        let webSocket = new WebSocket(ECHOENDPOINT_URL.replace(/^http/, "ws"));

        webSocket.onopen = () => {
            webSocket.send(message);
        };

        var received = "";
        webSocket.onmessage = event => {
            received += event.data;
            if (received === message) {
                webSocket.close();
            }
        };

        webSocket.onclose = event => {
            if (!event.wasClean) {
                fail("connection closed with unexpected status code: " + event.code + " " + event.reason);
            }

            // Jasmine doesn't like tests without expectations
            expect(event.wasClean).toBe(true);

            done();
        };
    });
});
