// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpTransportType, IHttpConnectionOptions, TransferFormat } from "@microsoft/signalr";
import { DEFAULT_TIMEOUT_INTERVAL, eachHttpClient, eachTransport, ECHOENDPOINT_URL, ENDPOINT_BASE_URL, HTTPS_ECHOENDPOINT_URL, shouldRunHttpsTests } from "./Common";
import { TestLogger } from "./TestLogger";

// We want to continue testing HttpConnection, but we don't export it anymore. So just pull it in directly from the source file.
import { HttpConnection } from "@microsoft/signalr/dist/esm/HttpConnection";
import { Platform } from "@microsoft/signalr/dist/esm/Utils";
import "./LogBannerReporter";
import { PromiseSource } from "./Utils";

jasmine.DEFAULT_TIMEOUT_INTERVAL = DEFAULT_TIMEOUT_INTERVAL;

const USED_ECHOENDPOINT_URL = shouldRunHttpsTests ? HTTPS_ECHOENDPOINT_URL : ECHOENDPOINT_URL;

const commonOptions: IHttpConnectionOptions = {
    logMessageContent: true,
    logger: TestLogger.instance,
};

describe("connection", () => {
    it("can connect to the server without specifying transport explicitly", async () => {
        const message = "Hello World!";
        const connection = new HttpConnection(USED_ECHOENDPOINT_URL, {
            ...commonOptions,
        });

        connection.onreceive = async (data: any) => {
            if (data === message) {
                await connection.stop();
            }
        };

        const closePromise = new PromiseSource();
        connection.onclose = (error: any) => {
            expect(error).toBeUndefined();
            closePromise.resolve();
        };

        await connection.start(TransferFormat.Text);

        await connection.send(message);

        await closePromise;
    });

    eachTransport((transportType) => {
        eachHttpClient((httpClient) => {
            describe(`over ${HttpTransportType[transportType]} with ${(httpClient.constructor as any).name}`, () => {
                it("can send and receive messages", async () => {
                    const message = "Hello World!";
                    // the url should be resolved relative to the document.location.host
                    // and the leading '/' should be automatically added to the url
                    const connection = new HttpConnection(USED_ECHOENDPOINT_URL, {
                        ...commonOptions,
                        httpClient,
                        transport: transportType,
                    });

                    connection.onreceive = async (data: any) => {
                        if (data === message) {
                            await connection.stop();
                        }
                    };

                    const closePromise = new PromiseSource();
                    connection.onclose = (error: any) => {
                        expect(error).toBeUndefined();
                        closePromise.resolve();
                    };

                    await connection.start(TransferFormat.Text);

                    await connection.send(message);

                    await closePromise;
                });

                it("does not log content of messages sent or received by default", async () => {
                    TestLogger.saveLogsAndReset();
                    const message = "Hello World!";

                    // DON'T use commonOptions because we want to specifically test the scenario where logMessageContent is not set.
                    const connection = new HttpConnection(USED_ECHOENDPOINT_URL, {
                        httpClient,
                        logger: TestLogger.instance,
                        transport: transportType,
                    });

                    const closePromise = new PromiseSource();
                    connection.onreceive = async (data: any) => {
                        if (data === message) {
                            await connection.stop();
                        }
                    };

                    // @ts-ignore: We don't use the error parameter intentionally.
                    connection.onclose = (error) => {
                        // Search the logs for the message content
                        expect(TestLogger.instance.currentLog.messages.length).toBeGreaterThan(0);
                        // @ts-ignore: We don't use the _ or __ parameters intentionally.
                        for (const [_1, _2, logMessage] of TestLogger.instance.currentLog.messages) {
                            expect(logMessage).not.toContain(message);
                        }
                        closePromise.resolve();
                    };

                    await connection.start(TransferFormat.Text);

                    await connection.send(message);

                    await closePromise;
                });

                it("does log content of messages sent or received when enabled", async () => {
                    TestLogger.saveLogsAndReset();
                    const message = "Hello World!";

                    // DON'T use commonOptions because we want to specifically test the scenario where logMessageContent is set to true (even if commonOptions changes).
                    const connection = new HttpConnection(USED_ECHOENDPOINT_URL, {
                        httpClient,
                        logMessageContent: true,
                        logger: TestLogger.instance,
                        transport: transportType,
                    });

                    connection.onreceive = async (data: any) => {
                        if (data === message) {
                            await connection.stop();
                        }
                    };

                    const closePromise = new PromiseSource();
                    // @ts-ignore: We don't use the error parameter intentionally.
                    connection.onclose = (error) => {
                        // Search the logs for the message content
                        let matches = 0;
                        expect(TestLogger.instance.currentLog.messages.length).toBeGreaterThan(0);
                        // @ts-ignore: We don't use the _ or __ parameters intentionally.
                        for (const [_1, _2, logMessage] of TestLogger.instance.currentLog.messages) {
                            if (logMessage.indexOf(message) !== -1) {
                                matches += 1;
                            }
                        }

                        // One match for send, one for receive.
                        expect(matches).toEqual(2);
                        closePromise.resolve();
                    };

                    await connection.start(TransferFormat.Text);

                    await connection.send(message);

                    await closePromise;
                });

                // withCredentials doesn't make sense in Node or when using WebSockets
                if (!Platform.isNode && transportType !== HttpTransportType.WebSockets &&
                    // tests run through karma during automation which is cross-site, but manually running the server will result in these tests failing
                    // so we check for cross-site
                    !(window && window.location.href.match(`^${ENDPOINT_BASE_URL}`))) {
                    it("honors withCredentials flag", async () => {
                        TestLogger.saveLogsAndReset();

                        // The server will set some response headers for the '/negotiate' endpoint
                        const connection = new HttpConnection(USED_ECHOENDPOINT_URL, {
                            ...commonOptions,
                            httpClient,
                            transport: transportType,
                            withCredentials: false,
                        });

                        connection.onreceive = (data: any) => {
                            fail(new Error(`Unexpected messaged received '${data}'.`));
                        };

                        const closePromise = new PromiseSource();
                        // @ts-ignore: We don't use the error parameter intentionally.
                        connection.onclose = (error) => {
                            closePromise.resolve();
                        };

                        await connection.start(TransferFormat.Text);

                        await connection.stop();

                        await closePromise;
                    });
                }
            });
        });
    });

    eachHttpClient((httpClient) => {
        describe(`with ${(httpClient.constructor as any).name}`, () => {
            it("follows HTTP redirects", async () => {
                const message = "Hello World!";
                const connection = new HttpConnection(USED_ECHOENDPOINT_URL + "redirect", {
                    ...commonOptions,
                    httpClient,
                });

                connection.onreceive = async (data: any) => {
                    if (data === message) {
                        await connection.stop();
                    }
                };

                const closePromise = new PromiseSource();
                connection.onclose = (error: any) => {
                    expect(error).toBeUndefined();
                    closePromise.resolve();
                };

                await connection.start(TransferFormat.Text);

                await connection.send(message);

                await closePromise;
            });

            it("contains server response in error", async () => {
                const connection = new HttpConnection(ENDPOINT_BASE_URL + "/bad-negotiate", {
                    ...commonOptions,
                    httpClient,
                });

                try {
                    await connection.start(TransferFormat.Text);
                    expect(true).toBe(false);
                } catch (e) {
                    expect(e).toEqual(new Error("Failed to complete negotiation with the server: Error: Some response from server: Status code '400'"));
                }
            })
        });
    });
});
