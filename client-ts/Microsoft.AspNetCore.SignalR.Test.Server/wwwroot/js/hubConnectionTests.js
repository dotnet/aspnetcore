const TESTHUBENDPOINT_URL = `http://${document.location.host}/testhub`;

describe('hubConnection', () => {
    eachTransport(transportName => {
        it(`over ${transportName} can invoke server method and receive result`, done => {
            const message = "Hi";
            let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');

            hubConnection.start(transportName)
                .then(() => {
                    hubConnection.invoke('Echo', message)
                    .then(result => {
                        expect(result).toBe(message);
                    })
                    .catch(() => {
                        fail();
                    })
                    .then(() => {
                        hubConnection.stop();
                        done();
                    })
                })
                .catch(() => {
                    fail();
                    done();
                })
        });

        it(`over ${transportName} rethrows an exception from the server`, done => {
            const errorMessage = "An error occurred.";
            let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');

            hubConnection.start(transportName)
                .then(() => {
                    hubConnection.invoke('ThrowException', errorMessage)
                        .then(() => {
                            // exception expected but none thrown
                            fail();
                        })
                        .catch(e => {
                            expect(e.message).toBe(errorMessage);
                        })
                        .then(() => {
                            hubConnection.stop();
                            done();
                        })
                })
                .catch(() => {
                    fail();
                    done();
                })
        });

        it(`over ${transportName} can receive server calls`, done => {
            let client = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');
            const message = "Hello SignalR";

            client.on("Message", msg => {
                expect(msg).toBe(message);
                client.stop();
                done();
            });

            client.start(transportName)
                .then(() => {
                    client.invoke('InvokeWithString', message)
                        .catch(e => {
                            fail();
                            client.stop();
                            done();
                        });
                })
                .catch(e => {
                    fail();
                    done();
                })
        });
    });
});