// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

"use strict";

describe('connection', function () {
    if (typeof WebSocket !== 'undefined') {
        it("can connect to the server without specifying transport explicitly", function (done) {
            var message = "Hello World!";
            var connection = new signalR.HttpConnection(ECHOENDPOINT_URL);

            var received = "";
            connection.onreceive = function (data) {
                received += data;
                if (data == message) {
                    connection.stop();
                }
            };

            connection.onclose = function (error) {
                expect(error).toBeUndefined();
                done();
            };

            connection.start().then(function () {
                connection.send(message);
            }).catch(function (e) {
                fail();
                done();
            });
        });
    }

    eachTransport(function (transportType) {
        it("over " + signalR.TransportType[transportType] + " can send and receive messages", function (done) {
            var message = "Hello World!";
            // the url should be resolved relative to the document.location.host
            // and the leading '/' should be automatically added to the url
            var connection = new signalR.HttpConnection("echo", {
                transport: transportType,
                logging: signalR.LogLevel.Trace
            });

            var received = "";
            connection.onreceive = function (data) {
                received += data;
                if (data == message) {
                    connection.stop();
                }
            };

            connection.onclose = function (error) {
                expect(error).toBeUndefined();
                done();
            };

            connection.start().then(function () {
                connection.send(message);
            }).catch(function (e) {
                fail();
                done();
            });
        });
    });
});
