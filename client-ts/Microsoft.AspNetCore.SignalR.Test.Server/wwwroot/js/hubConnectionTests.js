const TESTHUBENDPOINT_URL = `http://${document.location.host}/testhub`;

describe('hubConnection', () => {
    eachTransport(transportType => {
        it(`over ${signalR.TransportType[transportType]} can invoke server method and receive result`, done => {
            const message = "Hi";
            let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');
            hubConnection.onClosed = error => {
                expect(error).toBe(undefined);
                done();
            }

            hubConnection.start(transportType)
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

        it(`over ${signalR.TransportType[transportType]} rethrows an exception from the server`, done => {
            const errorMessage = "An error occurred.";
            let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');

            hubConnection.start(transportType)
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

        it(`over ${signalR.TransportType[transportType]} can receive server calls`, done => {
            let client = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');
            const message = "Hello SignalR";

            let callbackPromise = new Promise((resolve, reject) => {
                client.on("Message", msg => {
                    expect(msg).toBe(message);
                    resolve();
                });
            });

            client.start(transportType)
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