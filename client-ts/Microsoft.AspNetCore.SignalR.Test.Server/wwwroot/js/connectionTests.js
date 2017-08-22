describe('connection', () => {
    it(`can connect to the server without specifying transport explicitly`, done => {
        const message = "Hello World!";
        let connection = new signalR.HttpConnection(ECHOENDPOINT_URL);

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

        connection.start()
            .then(() => {
                connection.send(message);
            })
            .catch(e => {
                fail();
                done();
            });
    });

    eachTransport(transportType => {
        it(`over ${signalR.TransportType[transportType]} can send and receive messages`, done => {
            const message = "Hello World!";
            let connection = new signalR.HttpConnection(ECHOENDPOINT_URL,
                {
                    transport: transportType,
                    logger: new signalR.ConsoleLogger(signalR.LogLevel.Information)
                });

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

            connection.start()
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