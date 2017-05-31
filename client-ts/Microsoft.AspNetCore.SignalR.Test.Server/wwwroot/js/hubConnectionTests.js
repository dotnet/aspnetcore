const TESTHUBENDPOINT_URL = `http://${document.location.host}/testhub`;

describe('hubConnection', () => {
    eachTransport(transportType => {
        describe(`${signalR.TransportType[transportType]} transport`, () => {
            it(`can invoke server method and receive result`, done => {
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
                            .catch(e => {
                                fail(e);
                            })
                            .then(() => {
                                hubConnection.stop();
                            })
                    })
                    .catch(e => {
                        fail(e);
                        done();
                    });
            });

            it(`can stream server method and receive result`, done => {
                let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');
                hubConnection.onClosed = error => {
                    expect(error).toBe(undefined);
                    done();
                }

                let received = [];
                hubConnection.start(transportType)
                    .then(() => {
                        hubConnection.stream('Stream')
                            .subscribe({
                                next: (item) => {
                                    received.push(item);
                                },
                                error: (err) => {
                                    fail(err);
                                    done();
                                },
                                complete: () => {
                                    expect(received).toEqual(["a", "b", "c"]);
                                    done();
                                }
                            });
                    })
                    .catch(e => {
                        fail(e);
                        done();
                    });
            });

            it(`rethrows an exception from the server when invoking`, done => {
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
                                return hubConnection.stop();
                            })
                            .then(() => {
                                done();
                            });
                    })
                    .catch(e => {
                        fail(e);
                        done();
                    });
            });

            it(`rethrows an exception from the server when streaming`, done => {
                const errorMessage = "An error occurred.";
                let hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, 'formatType=json&format=text');

                hubConnection.start(transportType)
                    .then(() => {
                        hubConnection.stream('ThrowException', errorMessage)
                            .subscribe({
                                next: (item) => {
                                    fail();
                                },
                                error: (err) => {
                                    expect(err.message).toEqual("An error occurred.");
                                    done();
                                },
                                complete: () => {
                                    fail();
                                }
                            });

                    })
                    .catch(e => {
                        fail(e);
                        done();
                    });
            });

            it(`can receive server calls`, done => {
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
                        return stop();
                    })
                    .then(() => {
                        done();
                    })
                    .catch(e => {
                        fail(e);
                        done();
                    });
            });
        });
    });
});
