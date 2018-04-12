// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpConnection, HttpTransportType, IHttpConnectionOptions, LogLevel, TransferFormat } from "@aspnet/signalr";
import { eachTransport, ECHOENDPOINT_URL } from "./Common";
import { TestLogger } from "./TestLogger";

const commonOptions: IHttpConnectionOptions = {
    logMessageContent: true,
    logger: TestLogger.instance,
};

// On slower CI machines, these tests sometimes take longer than 5s
jasmine.DEFAULT_TIMEOUT_INTERVAL = 10 * 1000;

describe("connection", () => {
    it("can connect to the server without specifying transport explicitly", (done) => {
        const message = "Hello World!";
        const connection = new HttpConnection(ECHOENDPOINT_URL, {
            ...commonOptions,
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

        connection.start(TransferFormat.Text).then(() => {
            connection.send(message);
        }).catch((e) => {
            fail(e);
            done();
        });
    });

    eachTransport((transportType) => {
        describe(`over ${HttpTransportType[transportType]}`, () => {
            it("can send and receive messages", (done) => {
                const message = "Hello World!";
                // the url should be resolved relative to the document.location.host
                // and the leading '/' should be automatically added to the url
                const connection = new HttpConnection("echo", {
                    ...commonOptions,
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

                connection.start(TransferFormat.Text).then(() => {
                    connection.send(message);
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("does not log content of messages sent or received by default", (done) => {
                const message = "Hello World!";

                // DON'T use commonOptions because we want to specifically test the scenario where logMessageContent is not set.
                const connection = new HttpConnection("echo", {
                    logger: TestLogger.instance,
                    transport: transportType,
                });

                connection.onreceive = (data) => {
                    if (data === message) {
                        connection.stop();
                    }
                };

                connection.onclose = (error) => {
                    // Search the logs for the message content
                    expect(TestLogger.instance.currentLog.messages.length).toBeGreaterThan(0);
                    for (const [_, __, logMessage] of TestLogger.instance.currentLog.messages) {
                        expect(logMessage).not.toContain(message);
                    }
                    done();
                };

                connection.start(TransferFormat.Text).then(() => {
                    connection.send(message);
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });

            it("does log content of messages sent or received when enabled", (done) => {
                const message = "Hello World!";

                // DON'T use commonOptions because we want to specifically test the scenario where logMessageContent is set to true (even if commonOptions changes).
                const connection = new HttpConnection("echo", {
                    logMessageContent: true,
                    logger: TestLogger.instance,
                    transport: transportType,
                });

                connection.onreceive = (data) => {
                    if (data === message) {
                        connection.stop();
                    }
                };

                connection.onclose = (error) => {
                    // Search the logs for the message content
                    let matches = 0;
                    expect(TestLogger.instance.currentLog.messages.length).toBeGreaterThan(0);
                    for (const [_, __, logMessage] of TestLogger.instance.currentLog.messages) {
                        if (logMessage.indexOf(message) !== -1) {
                            matches += 1;
                        }
                    }

                    // One match for send, one for receive.
                    expect(matches).toEqual(2);
                    done();
                };

                connection.start(TransferFormat.Text).then(() => {
                    connection.send(message);
                }).catch((e) => {
                    fail(e);
                    done();
                });
            });
        });
    });
});
