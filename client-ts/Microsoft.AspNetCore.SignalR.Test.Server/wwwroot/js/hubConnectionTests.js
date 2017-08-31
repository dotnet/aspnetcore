// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

'use strict';

var TESTHUBENDPOINT_URL = 'http://' + document.location.host + '/testhub';

describe('hubConnection', function () {
    eachTransportAndProtocol(function (transportType, protocol) {
        describe(protocol.name + ' over ' + signalR.TransportType[transportType] + ' transport', function () {

            it('can invoke server method and receive result', function (done) {
                var message = "你好，世界！";
                var hubConnection = new signalR.HubConnection(new signalR.HttpConnection(TESTHUBENDPOINT_URL, { transport: transportType }), { protocol: protocol});
                hubConnection.onClosed = function (error) {
                    expect(error).toBe(undefined);
                    done();
                };

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

            it('can stream server method and receive result', function (done) {
                var hubConnection = new signalR.HubConnection(new signalR.HttpConnection(TESTHUBENDPOINT_URL, { transport: transportType }), { protocol: protocol});

                hubConnection.onClosed = function (error) {
                    expect(error).toBe(undefined);
                    done();
                };

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
                            expect(received).toEqual(["a", "b", "c"]);
                            hubConnection.stop();
                        }
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('rethrows an exception from the server when invoking', function (done) {
                var errorMessage = "An error occurred.";
                var hubConnection = new signalR.HubConnection(new signalR.HttpConnection(TESTHUBENDPOINT_URL, { transport: transportType }), { protocol: protocol});

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

            it('rethrows an exception from the server when streaming', function (done) {
                var errorMessage = "An error occurred.";

                var hubConnection = new signalR.HubConnection(new signalR.HttpConnection(TESTHUBENDPOINT_URL, { transport: transportType }), { protocol: protocol});

                hubConnection.start().then(function () {
                    hubConnection.stream('ThrowException', errorMessage).subscribe({
                        next: function next(item) {
                            fail();
                        },
                        error: function error(err) {
                            expect(err.message).toEqual("An error occurred.");
                            done();
                        },
                        complete: function complete() {
                            fail();
                        }
                    });
                }).catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('can receive server calls', function (done) {
                var hubConnection = new signalR.HubConnection(new signalR.HttpConnection(TESTHUBENDPOINT_URL, { transport: transportType }), { protocol: protocol});

                var message = "你好 SignalR！";

                hubConnection.on("Message", function (msg) {
                    expect(msg).toBe(message);
                    done();
                });

                hubConnection.start().then(function () {
                    return hubConnection.invoke('InvokeWithString', message);
                })
                .then(function() {
                    return hubConnection.stop();
                })
                .catch(function (e) {
                    fail(e);
                    done();
                });
            });

            it('closed with error if hub cannot be created', function (done) {
                var errorRegex = {
                    WebSockets: "1011|1005", // Message is browser specific (e.g. 'Websocket closed with status code: 1011'), Edge and IE report 1005 even though the server sent 1011
                    LongPolling: "Internal Server Error",
                    ServerSentEvents: "Error occurred"
                };

                var hubConnection = new signalR.HubConnection(new signalR.HttpConnection('http://' + document.location.host + '/uncreatable', { transport: transportType }), { protocol: protocol});

                hubConnection.onClosed = function (error) {
                    expect(error.message).toMatch(errorRegex[signalR.TransportType[transportType]]);
                    done();
                };
                hubConnection.start();
            });
        });
    });
});
