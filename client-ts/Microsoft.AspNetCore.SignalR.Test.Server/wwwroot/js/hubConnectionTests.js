const TESTHUBENDPOINT_URL = `http://${document.location.host}/testhub`;

describe('hubConnection', () => {
    eachTransport(transportName => {
        it(`over ${transportName} can invoke server method and receive result`, done => {
            const message = "Hi";
            let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');
            hubConnection.onClosed = error => {
                expect(error).toBe(undefined);
                done();
            }

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
                    })
                })
                .catch(() => {
                    fail();
                    done();
                });
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
                });
        });

        it(`over ${transportName} can receive server calls`, done => {
            let client = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');
            const message = "Hello SignalR";

            let callbackPromise = new Promise((resolve, reject) => {
                client.on("Message", msg => {
                    expect(msg).toBe(message);
                    resolve();
                });
            });

            client.start(transportName)
                .then(() => {
                    return Promise.all([client.invoke('InvokeWithString', message), callbackPromise]);
                })
                .then(() => {
                    stop();
                    done();
                })
                .catch(e => {
                    fail();
                    done();
                });
        });
    });
});