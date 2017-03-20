describe('connection', () => {
    eachTransport(transportName => {
        it(`over ${transportName} can send and receive messages`, done => {
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

            connection.start(transportName)
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