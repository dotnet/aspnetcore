// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpConnection, LogLevel, TransportType } from "@aspnet/signalr";
import { eachTransport, ECHOENDPOINT_URL } from "./Common";

describe("connection", () => {
    if (typeof WebSocket !== "undefined") {
        it("can connect to the server without specifying transport explicitly", (done) => {
            const message = "Hello World!";
            const connection = new HttpConnection(ECHOENDPOINT_URL);

            let received = "";
            connection.onreceive = (data) => {
                received += data;
                if (data === message) {
                    connection.stop();
                }
            };

            connection.onclose = (error) => {
                expect(error).toBeUndefined();
                done();
            };

            connection.start().then(() => {
                connection.send(message);
            }).catch((e) => {
                fail();
                done();
            });
        });
    }

    eachTransport((transportType) => {
        it("over " + TransportType[transportType] + " can send and receive messages", (done) => {
            const message = "Hello World!";
            // the url should be resolved relative to the document.location.host
            // and the leading '/' should be automatically added to the url
            const connection = new HttpConnection("echo", {
                logger: LogLevel.Trace,
                transport: transportType,
            });

            let received = "";
            connection.onreceive = (data) => {
                received += data;
                if (data === message) {
                    connection.stop();
                }
            };

            connection.onclose = (error) => {
                expect(error).toBeUndefined();
                done();
            };

            connection.start().then(() => {
                connection.send(message);
            }).catch((e) => {
                fail();
                done();
            });
        });
    });
});
