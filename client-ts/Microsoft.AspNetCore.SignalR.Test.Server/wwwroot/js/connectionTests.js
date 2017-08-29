"use strict";

describe('connection', function () {
    it("can connect to the server without specifying transport explicitly", function (done) {
        var message = "Hello World!";
        var connection = new signalR.HttpConnection(ECHOENDPOINT_URL);

        var received = "";
        connection.onDataReceived = function (data) {
            received += data;
            if (data == message) {
                connection.stop();
            }
        };

        connection.onClosed = function (error) {
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

    eachTransport(function (transportType) {
        it("over " + signalR.TransportType[transportType] + " can send and receive messages", function (done) {
            var message = "Hello World!";
            var connection = new signalR.HttpConnection(ECHOENDPOINT_URL, {
                transport: transportType,
                logger: signalR.LogLevel.Information
            });

            var received = "";
            connection.onDataReceived = function (data) {
                received += data;
                if (data == message) {
                    connection.stop();
                }
            };

            connection.onClosed = function (error) {
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
