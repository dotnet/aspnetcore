const ECHOENDPOINT_URL = `ws://${document.location.host}/echo/ws`;

describe('WebSockets', function () {
    it('can be used to connect to SignalR', done => {
        const message = "message";

        let webSocket = new WebSocket(ECHOENDPOINT_URL);

        webSocket.onopen = () => {
            webSocket.send(message);
        };

        webSocket.onerror = event => {
            expect(true).toBe(false);
            done();
        };

        var received = "";
        webSocket.onmessage = event => {
            received += event.data;
            if (received === message) {
                webSocket.close();
            }
        }

        webSocket.onclose = event => {
            expect(event.wasClean).toBe(true);
            done();
        }
    });
});