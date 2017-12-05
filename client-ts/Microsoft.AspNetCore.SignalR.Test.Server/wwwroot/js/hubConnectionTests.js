// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

'use strict';

var TESTHUBENDPOINT_URL = '/testhub';

describe('hubConnection', function () {
    eachTransportAndProtocol(function (transportType, protocol) {
        describe(protocol.name + ' over ' + signalR.TransportType[transportType] + ' transport', function () {
            it('can invoke server method and receive result', function (done) {
                var message = '你好，世界！';

                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);
                hubConnection.onclose(function (error) {
                    expect(error).toBe(undefined);
                    done();
                });

                hubConnection.start().then(function () {
                    hubConnection.invoke('Echo', message).then(function (result) {
                        expect(result).toBe(message);
                    }).catch(function (e) {
                        fail(e);
                    }).then(function () {
                        hubConnection.stop();
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('can invoke server method structural object and receive structural result', function (done) {
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.on('CustomObject', function (customObject) {
                    expect(customObject.Name).toBe('test');
                    expect(customObject.Value).toBe(42);
                    hubConnection.stop();
                });

                hubConnection.onclose(function (error) {
                    expect(error).toBe(undefined);
                    done();
                });

                hubConnection.start().then(function () {
                    hubConnection.send('SendCustomObject', { Name: 'test', Value: 42 });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('can stream server method and receive result', function (done) {

                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.onclose(function (error) {
                    expect(error).toBe(undefined);
                    done();
                });

                var received = [];
                hubConnection.start().then(function () {
                    hubConnection.stream('Stream').subscribe({
                        next: function next(item) {
                            received.push(item);
                        },
                        error: function error(err) {
                            fail(err);
                            hubConnection.stop();
                        },
                        complete: function complete() {
                            expect(received).toEqual(['a', 'b', 'c']);
                            hubConnection.stop();
                        }
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('rethrows an exception from the server when invoking', function (done) {
                var errorMessage = 'An error occurred.';
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.start().then(function () {
                    hubConnection.invoke('ThrowException', errorMessage).then(function () {
                        // exception expected but none thrown
                        fail();
                    }).catch(function (e) {
                        expect(e.message).toBe(errorMessage);
                    }).then(function () {
                        return hubConnection.stop();
                    }).then(function () {
                        done();
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('throws an exception when invoking streaming method with invoke', function (done) {
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.start().then(function () {
                    hubConnection.invoke('EmptyStream').then(function () {
                        // exception expected but none thrown
                        fail();
                    }).catch(function (e) {
                        expect(e.message).toBe('The client attempted to invoke the streaming \'EmptyStream\' method in a non-streaming fashion.');
                    }).then(function () {
                        return hubConnection.stop();
                    }).then(function () {
                        done();
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('throws an exception when receiving a streaming result for method called with invoke', function (done) {
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.start().then(function () {
                    hubConnection.invoke('Stream').then(function () {
                        // exception expected but none thrown
                        fail();
                    }).catch(function (e) {
                        expect(e.message).toBe('The client attempted to invoke the streaming \'Stream\' method in a non-streaming fashion.');
                    }).then(function () {
                        return hubConnection.stop();
                    }).then(function () {
                        done();
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('rethrows an exception from the server when streaming', function (done) {
                var errorMessage = 'An error occurred.';
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.start().then(function () {
                    hubConnection.stream('StreamThrowException', errorMessage).subscribe({
                        next: function next(item) {
                            hubConnection.stop();
                            fail();
                        },
                        error: function error(err) {
                            expect(err.message).toEqual('An error occurred.');
                            hubConnection.stop();
                            done();
                        },
                        complete: function complete() {
                            hubConnection.stop();
                            fail();
                        }
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('throws an exception when invoking hub method with stream', function (done) {
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                hubConnection.start().then(function () {
                    hubConnection.stream('Echo', '42').subscribe({
                        next: function next(item) {
                            hubConnection.stop();
                            fail();
                        },
                        error: function error(err) {
                            expect(err.message).toEqual('The client attempted to invoke the non-streaming \'Echo\' method in a streaming fashion.');
                            hubConnection.stop();
                            done();
                        },
                        complete: function complete() {
                            hubConnection.stop();
                            fail();
                        }
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('can receive server calls', function (done) {
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                var message = '你好 SignalR！';

                // client side method names are case insensitive
                var methodName = 'message';
                var idx = Math.floor(Math.random() * (methodName.length - 1));
                methodName = methodName.substr(0, idx) + methodName[idx].toUpperCase() + methodName.substr(idx + 1);

                hubConnection.on(methodName, function (msg) {
                    expect(msg).toBe(message);
                    done();
                });

                hubConnection.start().then(function () {
                    return hubConnection.invoke('InvokeWithString', message);
                })
                .then(function () {
                    return hubConnection.stop();
                })
                .catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('closed with error if hub cannot be created', function (done) {
                var errorRegex = {
                    WebSockets: '1011|1005', // Message is browser specific (e.g. 'Websocket closed with status code: 1011'), Edge and IE report 1005 even though the server sent 1011
                    LongPolling: 'Internal Server Error',
                    ServerSentEvents: 'Error occurred'
                };

                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };
                var hubConnection = new signalR.HubConnection('http://' + document.location.host + '/uncreatable', options);

                hubConnection.onclose(function (error) {
                    expect(error.message).toMatch(errorRegex[signalR.TransportType[transportType]]);
                    done();
                });
                hubConnection.start();
            });

            it('can handle different types', function (done) {
                var options = {
                    transport: transportType,
                    protocol: protocol,
                    logging: signalR.LogLevel.Trace
                };

                var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);
                hubConnection.onclose(function (error) {
                    expect(error).toBe(undefined);
                    done();
                });

                var complexObject = {
                    String: 'Hello, World!',
                    IntArray: [0x01, 0x02, 0x03, 0xff],
                    ByteArray: protocol.name === "json"
                        ? btoa([0xff, 0x03, 0x02, 0x01])
                        : new Uint8Array([0xff, 0x03, 0x02, 0x01]),
                    GUID: protocol.name === "json"
                        ? "00010203-0405-0607-0706-050403020100"
                        : new Uint8Array([0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00])
                };

                hubConnection.start().then(function () {
                    return hubConnection.invoke('EchoComplexObject', complexObject);
                })
                .then(function (value) {
                    if (protocol.name === "messagepack") {
                        // msgpack creates a Buffer for byte arrays and jasmine fails to compare a Buffer
                        // and a Uint8Array even though Buffer instances are also Uint8Array instances
                        value.ByteArray = new Uint8Array(value.ByteArray);

                        // GUIDs are serialized as raw type which is a string containing bytes which need to
                        // be extracted. Note that with msgpack5 the original bytes will be encoded with utf8
                        // and needs to be decoded. To not go into utf8 encoding intricacies the test uses values
                        // less than 0x80.
                        let guidBytes = [];
                        for (let i = 0; i < value.GUID.length; i++) {
                            guidBytes.push(value.GUID.charCodeAt(i));
                        }
                        value.GUID = new Uint8Array(guidBytes);
                    }
                    expect(value).toEqual(complexObject);
                })
                .then(function () {
                    hubConnection.stop();
                })
                .catch(function (e) {
                    fail(e);
                    done();
                });
            });
        });
    });

    eachTransport(function (transportType) {
        describe(' over ' + signalR.TransportType[transportType] + ' transport', function () {

            it('can connect to hub with authorization', function (done) {
                var message = '你好，世界！';

                var hubConnection;
                getJwtToken('http://' + document.location.host + '/generateJwtToken')
                    .then(jwtToken => {
                        var options = {
                            transport: transportType,
                            logging: signalR.LogLevel.Trace,
                            jwtBearer: function () {
                                return jwtToken;
                            }
                        };
                        hubConnection = new signalR.HubConnection('/authorizedhub', options);
                        hubConnection.onclose(function (error) {
                            expect(error).toBe(undefined);
                            done();
                        });
                        return hubConnection.start();
                    })
                    .then(function () {
                        return hubConnection.invoke('Echo', message);
                    })
                    .then(function (response) {
                        expect(response).toEqual(message);
                        return hubConnection.stop();
                    })
                    .catch(function (e) {
                        fail(e);
                        done();
                    });
            });

            if (transportType != signalR.TransportType.LongPolling) {
                it("terminates if no messages received within timeout interval", function (done) {
                    var options = {
                        transport: transportType,
                        logging: signalR.LogLevel.Trace,
                        serverTimeoutInMilliseconds: 100
                    };

                    var hubConnection = new signalR.HubConnection(TESTHUBENDPOINT_URL, options);

                    var timeout = setTimeout(200, function () {
                        fail("Server timeout did not fire within expected interval");
                    });

                    hubConnection.start().then(function () {
                        hubConnection.onclose(function (error) {
                            clearTimeout(timeout);
                            expect(error).toEqual(new Error("Server timeout elapsed without receiving a message from the server."));
                            done();
                        });
                    });
                });
            }
        });
    });

    function getJwtToken(url) {
        return new Promise((resolve, reject) => {
            let xhr = new XMLHttpRequest();

            xhr.open('GET', url, true);
            xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
            xhr.send();
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(xhr.response || xhr.responseText);
                }
                else {
                    reject(new Error(xhr.statusText));
                }
            };

            xhr.onerror = () => {
                reject(new Error(xhr.statusText));
            }
        });
    }
});
