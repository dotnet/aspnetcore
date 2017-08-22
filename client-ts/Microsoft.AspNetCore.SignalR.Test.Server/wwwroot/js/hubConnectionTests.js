const TESTHUBENDPOINT_URL = `http://${document.location.host}/testhub`;

describe('hubConnection', () => {
    eachTransportAndProtocol((transportType, protocol) => {
        describe(`${protocol.name} over ${signalR.TransportType[transportType]} transport`, () => {
            it(`can invoke server method and receive result`, done => {
                const message = "你好，世界！";
                let logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
                let hubConnection = new signalR.HubConnection(new signalR.HttpConnection(
                        TESTHUBENDPOINT_URL,
                        { transport: transportType, logger: logger }),
                        logger,
                        protocol);
                hubConnection.onClosed = error => {
                    expect(error).toBe(undefined);
                    done();
                }

                hubConnection.start()
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
                let logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
                let hubConnection = new signalR.HubConnection(new signalR.HttpConnection(
                    TESTHUBENDPOINT_URL,
                    { transport: transportType, logger: logger }),
                    logger,
                    protocol);

                hubConnection.onClosed = error => {
                    expect(error).toBe(undefined);
                    done();
                }

                let received = [];
                hubConnection.start()
                    .then(() => {
                        hubConnection.stream('Stream')
                            .subscribe({
                                next: (item) => {
                                    received.push(item);
                                },
                                error: (err) => {
                                    fail(err);
                                    hubConnection.stop();
                                },
                                complete: () => {
                                    expect(received).toEqual(["a", "b", "c"]);
                                    hubConnection.stop();
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

                let logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
                let hubConnection = new signalR.HubConnection(new signalR.HttpConnection(
                    TESTHUBENDPOINT_URL,
                    { transport: transportType, logger: logger }),
                    logger,
                    protocol);

                hubConnection.start()
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

                let logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
                let hubConnection = new signalR.HubConnection(new signalR.HttpConnection(
                    TESTHUBENDPOINT_URL,
                    { transport: transportType, logger: logger }),
                    logger,
                    protocol);

                hubConnection.start()
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
                let logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
                let hubConnection = new signalR.HubConnection(new signalR.HttpConnection(
                    TESTHUBENDPOINT_URL,
                    { transport: transportType, logger: logger }),
                    logger,
                    protocol);

                const message = "你好 SignalR！";

                let callbackPromise = new Promise((resolve, reject) => {
                    hubConnection.on("Message", msg => {
                        expect(msg).toBe(message);
                        resolve();
                    });
                });

                hubConnection.start()
                    .then(() => {
                        return Promise.all([hubConnection.invoke('InvokeWithString', message), callbackPromise]);
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

            it(`closed with error if hub cannot be created`, done => {
                let errorRegex = {
                    WebSockets: "1011", // Message is browser specific (e.g. 'Websocket closed with status code: 1011')
                    LongPolling: "Internal Server Error",
                    ServerSentEvents: "Error occurred"
                };

                let logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
                let hubConnection = new signalR.HubConnection(new signalR.HttpConnection(
                    `http://${document.location.host}/uncreatable`,
                    { transport: transportType, logger: logger }),
                    logger,
                    protocol);

                hubConnection.onClosed = error => {
                    expect(error.message).toMatch(errorRegex[signalR.TransportType[transportType]]);
                    done();
                }
                hubConnection.start();
            });
        });
    });
});
