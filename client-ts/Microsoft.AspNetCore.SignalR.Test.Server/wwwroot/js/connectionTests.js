describe('connection', () => {
    eachTransport(transportType => {
        it(`over ${signalR.TransportType[transportType]} can send and receive messages`, done => {
            const message = "Hello World!";
            let connection = new signalR.Connection(ECHOENDPOINT_URL);

            let received = "";
            connection.onDataReceived = data => {
                received += data;
                if (data == message) {
                    connection.stop();
                }
            }

            connection.onClosed = error => {
                expect(error).toBeUndefined();
                done();
            }

            connection.start(transportType)
                .then(() => {
                    connection.send(message);
                })
                .catch(e => {
                    fail();
                    done();
                });
        });
    });
});