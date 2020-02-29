// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This code uses a lot of `.then` instead of `await` and TSLint doesn't like it.
// tslint:disable:no-floating-promises

import { HttpTransportType, IHttpConnectionOptions, TransferFormat } from "@microsoft/signalr";
import { eachHttpClient, eachTransport, ECHOENDPOINT_URL } from "./Common";
import { TestLogger } from "./TestLogger";

// We want to continue testing HttpConnection, but we don't export it anymore. So just pull it in directly from the source file.
import { HttpConnection } from "@microsoft/signalr/dist/esm/HttpConnection";
import { Platform } from "@microsoft/signalr/dist/esm/Utils";
import "./LogBannerReporter";

const commonOptions: IHttpConnectionOptions = {
    logMessageContent: true,
    logger: TestLogger.instance,
};

describe("connection", () => {
    it("can connect to the server without specifying transport explicitly", (done) => {
        const message = "Hello World!";
        const connection = new HttpConnection(ECHOENDPOINT_URL, {
            ...commonOptions,
        });

        connection.onreceive = async (data: any) => {
            if (data === message) {
                connection.stop();
            }
        };

        connection.onclose = (error: any) => {
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
        eachHttpClient((httpClient) => {
            describe(`over ${HttpTransportType[transportType]} with ${(httpClient.constructor as any).name}`, () => {
                it("can send and receive messages", (done) => {
                    const message = "Hello World!";
                    // the url should be resolved relative to the document.location.host
                    // and the leading '/' should be automatically added to the url
                    const connection = new HttpConnection(ECHOENDPOINT_URL, {
                        ...commonOptions,
                        httpClient,
                        transport: transportType,
                    });

                    connection.onreceive = (data: any) => {
                        if (data === message) {
                            connection.stop();
                        }
                    };

                    connection.onclose = (error: any) => {
                        expect(error).toBeUndefined();
                        done();
                    };

                    connection.start(TransferFormat.Text).then(() => {
                        connection.send(message);
                    }).catch((e: any) => {
                        fail(e);
                        done();
                    });
                });

                it("does not log content of messages sent or received by default", (done) => {
                    TestLogger.saveLogsAndReset();
                    const message = "Hello World!";

                    // DON'T use commonOptions because we want to specifically test the scenario where logMessageContent is not set.
                    const connection = new HttpConnection(ECHOENDPOINT_URL, {
                        httpClient,
                        logger: TestLogger.instance,
                        transport: transportType,
                    });

                    connection.onreceive = (data: any) => {
                        if (data === message) {
                            connection.stop();
                        }
                    };

                    // @ts-ignore: We don't use the error parameter intentionally.
                    connection.onclose = (error) => {
                        // Search the logs for the message content
                        expect(TestLogger.instance.currentLog.messages.length).toBeGreaterThan(0);
                        // @ts-ignore: We don't use the _ or __ parameters intentionally.
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
                    TestLogger.saveLogsAndReset();
                    const message = "Hello World!";

                    // DON'T use commonOptions because we want to specifically test the scenario where logMessageContent is set to true (even if commonOptions changes).
                    const connection = new HttpConnection(ECHOENDPOINT_URL, {
                        httpClient,
                        logMessageContent: true,
                        logger: TestLogger.instance,
                        transport: transportType,
                    });

                    connection.onreceive = (data: any) => {
                        if (data === message) {
                            connection.stop();
                        }
                    };

                    // @ts-ignore: We don't use the error parameter intentionally.
                    connection.onclose = (error) => {
                        // Search the logs for the message content
                        let matches = 0;
                        expect(TestLogger.instance.currentLog.messages.length).toBeGreaterThan(0);
                        // @ts-ignore: We don't use the _ or __ parameters intentionally.
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
                    }).catch((e: any) => {
                        fail(e);
                        done();
                    });
                });

                // withCredentials doesn't make sense in Node or when using WebSockets
                if (!Platform.isNode && transportType !== HttpTransportType.WebSockets &&
                    // tests run through karma during automation which is cross-site, but manually running the server will result in these tests failing
                    // so we check for cross-site
                    !(window && ECHOENDPOINT_URL.match(`^${window.location.href}`))) {
                    it("honors withCredentials flag", (done) => {
                        TestLogger.saveLogsAndReset();
                        const message = "Hello World!";

                        // The server will set some response headers for the '/negotiate' endpoint
                        const connection = new HttpConnection(ECHOENDPOINT_URL, {
                            ...commonOptions,
                            httpClient,
                            transport: transportType,
                            withCredentials: false,
                        });

                        connection.onreceive = (data: any) => {
                            fail(new Error(`Unexpected messaged received '${data}'.`));
                        };

                        // @ts-ignore: We don't use the error parameter intentionally.
                        connection.onclose = (error) => {
                            done();
                        };

                        connection.start(TransferFormat.Text).then(() => {
                            connection.send(message);
                        }).catch((e: any) => {
                            fail(e);
                            done();
                        });
                    });
                }
            });
        });
    });
});
