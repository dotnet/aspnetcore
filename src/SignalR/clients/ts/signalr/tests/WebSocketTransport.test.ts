// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HeaderNames } from "../src/HeaderNames";
import { MessageHeaders } from "../src/IHubProtocol";
import { ILogger } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
import { getUserAgentHeader } from "../src/Utils";
import { WebSocketTransport } from "../src/WebSocketTransport";
import { VerifyLogger } from "./Common";
import { TestMessageEvent } from "./TestEventSource";
import { TestHttpClient } from "./TestHttpClient";
import { TestCloseEvent, TestErrorEvent, TestEvent, TestWebSocket } from "./TestWebSocket";
import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

describe("WebSocketTransport", () => {
    it("sets websocket binarytype to arraybuffer on Binary transferformat", async () => {
        await VerifyLogger.run(async (logger) => {
            await createAndStartWebSocket(logger, "http://example.com", undefined, TransferFormat.Binary);

            expect(TestWebSocket.webSocket.binaryType).toBe("arraybuffer");
        });
    });

    it("connect waits for WebSocket to be connected", async () => {
        await VerifyLogger.run(async (logger) => {
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket, {});

            let connectComplete: boolean = false;
            const connectPromise = (async () => {
                await webSocket.connect("http://example.com", TransferFormat.Text);
                connectComplete = true;
            })();

            await TestWebSocket.webSocket.openSet;

            expect(connectComplete).toBe(false);

            TestWebSocket.webSocket.onopen(new TestEvent());

            await connectPromise;
            expect(connectComplete).toBe(true);
        });
    });

    it("connect fails if there is error during connect", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestErrorEvent;
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket, {});

            let connectComplete: boolean = false;
            const connectPromise = (async () => {
                await webSocket.connect("http://example.com", TransferFormat.Text);
                connectComplete = true;
            })();

            await TestWebSocket.webSocket.closeSet;

            expect(connectComplete).toBe(false);

            TestWebSocket.webSocket.onclose(new TestEvent());

            await expect(connectPromise)
                .rejects
                .toThrow("WebSocket failed to connect. The connection could not be found on the server, either the endpoint may not be a SignalR endpoint, " +
                "the connection ID is not present on the server, or there is a proxy blocking WebSockets. If you have multiple servers check that sticky sessions are enabled.");
            expect(connectComplete).toBe(false);
        });
    });

    it("connect failure does not call onclose handler", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestErrorEvent;
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket, {});
            let closeCalled = false;
            webSocket.onclose = () => closeCalled = true;

            let connectComplete: boolean = false;
            const connectPromise = (async () => {
                await webSocket.connect("http://example.com", TransferFormat.Text);
                connectComplete = true;
            })();

            await TestWebSocket.webSocket.closeSet;

            expect(connectComplete).toBe(false);

            TestWebSocket.webSocket.onclose(new TestEvent());

            await expect(connectPromise)
                .rejects
                .toThrow("WebSocket failed to connect. The connection could not be found on the server, either the endpoint may not be a SignalR endpoint, " +
                "the connection ID is not present on the server, or there is a proxy blocking WebSockets. If you have multiple servers check that sticky sessions are enabled.");
            expect(connectComplete).toBe(false);
            expect(closeCalled).toBe(false);
        });
    });

    [[undefined as any, undefined],
    ["secretToken", "Bearer secretToken"],
    ["", undefined]]
        .forEach(([input, expected]) => {
            it(`sets Authorization header with ${input} correctly`, async () => {
                await VerifyLogger.run(async (logger) => {
                    await createAndStartWebSocket(logger, "http://example.com", () => input);

                    expect(TestWebSocket.webSocket.url).toBe("ws://example.com");
                    if (expected) {
                        expect(TestWebSocket.webSocket.options.headers[HeaderNames.Authorization]).toBe(expected);
                    } else {
                        expect(TestWebSocket.webSocket.options.headers[HeaderNames.Authorization]).toBeUndefined();
                    }
                });
            });
        });

    [["http://example.com", "ws://example.com"],
    ["http://example.com?value=null", "ws://example.com?value=null"],
    ["https://example.com?value=null", "wss://example.com?value=null"]]
        .forEach(([input, expected]) => {
            it(`generates correct WebSocket URL for ${input}`, async () => {
                await VerifyLogger.run(async (logger) => {
                    await createAndStartWebSocket(logger, input, undefined);

                    expect(TestWebSocket.webSocket.url).toBe(expected);
                });
            });
        });

    it("can receive data", async () => {
        await VerifyLogger.run(async (logger) => {
            const webSocket = await createAndStartWebSocket(logger);

            let received: string | ArrayBuffer;
            webSocket.onreceive = (data) => {
                received = data;
            };

            const message = new TestMessageEvent();
            message.data = "receive data";
            TestWebSocket.webSocket.onmessage(message);

            expect(typeof received!).toBe("string");
            expect(received!).toBe("receive data");
        });
    });

    it("is closed from WebSocket onclose with error", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestEvent;
            const webSocket = await createAndStartWebSocket(logger);

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            const message = new TestCloseEvent();
            message.wasClean = false;
            message.code = 1;
            message.reason = "just cause";
            TestWebSocket.webSocket.onclose(message);

            expect(closeCalled).toBe(true);
            expect(error!).toEqual(new Error("WebSocket closed with status code: 1 (just cause)."));

            await expect(webSocket.send(""))
                .rejects
                .toBe("WebSocket is not in the OPEN state");
        });
    });

    it("is closed from WebSocket onclose", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestEvent;
            const webSocket = await createAndStartWebSocket(logger);

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            const message = new TestCloseEvent();
            message.wasClean = true;
            message.code = 1000;
            message.reason = "success";
            TestWebSocket.webSocket.onclose(message);

            expect(closeCalled).toBe(true);
            expect(error!).toBeUndefined();

            await expect(webSocket.send(""))
                .rejects
                .toBe("WebSocket is not in the OPEN state");
        });
    });

    it("is closed from Transport stop", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestEvent;
            const webSocket = await createAndStartWebSocket(logger);

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            await webSocket.stop();

            expect(closeCalled).toBe(true);
            expect(error!).toBeUndefined();

            await expect(webSocket.send(""))
                .rejects
                .toBe("WebSocket is not in the OPEN state");
        });
    });

    [[TransferFormat.Text, "send data"],
    [TransferFormat.Binary, new Uint8Array([0, 1, 3])]]
        .forEach(([format, data]) => {
            it(`can send ${TransferFormat[format as TransferFormat]} data`, async () => {
                await VerifyLogger.run(async (logger) => {
                    const webSocket = await createAndStartWebSocket(logger, "http://example.com", undefined, format as TransferFormat);

                    TestWebSocket.webSocket.readyState = TestWebSocket.OPEN;
                    await webSocket.send(data);

                    expect(TestWebSocket.webSocket.receivedData.length).toBe(1);
                    expect(TestWebSocket.webSocket.receivedData[0]).toBe(data);
                });
            });
        });

    it("sets user agent header on connect", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestEvent;
            const webSocket = await createAndStartWebSocket(logger);

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            const [, value] = getUserAgentHeader();
            expect(TestWebSocket.webSocket.options!.headers[`User-Agent`]).toEqual(value);

            await webSocket.stop();

            expect(closeCalled).toBe(true);
            expect(error!).toBeUndefined();

            await expect(webSocket.send(""))
                .rejects
                .toBe("WebSocket is not in the OPEN state");
        });
    });

    it("overwrites library headers with user headers", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestEvent;
            const headers = { "User-Agent": "Custom Agent", "X-HEADER": "VALUE" };
            const webSocket = await createAndStartWebSocket(logger, undefined, undefined, undefined, headers);

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            expect(TestWebSocket.webSocket.options!.headers[`User-Agent`]).toEqual("Custom Agent");
            expect(TestWebSocket.webSocket.options!.headers[`X-HEADER`]).toEqual("VALUE");

            await webSocket.stop();

            expect(closeCalled).toBe(true);
            expect(error!).toBeUndefined();

            await expect(webSocket.send(""))
                .rejects
                .toBe("WebSocket is not in the OPEN state");
        });
    });

    it("is closed from 'onreceive' callback throwing", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestEvent;
            const webSocket = await createAndStartWebSocket(logger);

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            const receiveError = new Error("callback error");
            webSocket.onreceive = () => {
                throw receiveError;
            };

            const message = new TestMessageEvent();
            message.data = "receive data";
            TestWebSocket.webSocket.onmessage(message);

            expect(closeCalled).toBe(true);
            expect(error!).toBe(receiveError);

            await expect(webSocket.send(""))
                .rejects
                .toBe("WebSocket is not in the OPEN state");
        });
    });

    it("does not run onclose callback if Transport does not fully connect and exits", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestErrorEvent;
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket, {});

            const connectPromise = webSocket.connect("http://example.com", TransferFormat.Text);

            await TestWebSocket.webSocket.closeSet;

            let closeCalled: boolean = false;
            let error: Error;
            webSocket.onclose = (e) => {
                closeCalled = true;
                error = e!;
            };

            const message = new TestCloseEvent();
            message.wasClean = false;
            message.code = 1;
            message.reason = "just cause";
            TestWebSocket.webSocket.onclose(message);

            expect(closeCalled).toBe(false);
            expect(error!).toBeUndefined();

            await expect(connectPromise).rejects.toThrow("WebSocket failed to connect. The connection could not be found on the server, " +
            "either the endpoint may not be a SignalR endpoint, the connection ID is not present on the server, or there is a proxy blocking WebSockets. If you have multiple servers check that sticky sessions are enabled.");
        });
    });
});

async function createAndStartWebSocket(logger: ILogger, url?: string, accessTokenFactory?: (() => string | Promise<string>), format?: TransferFormat, headers?: MessageHeaders): Promise<WebSocketTransport> {
    const webSocket = new WebSocketTransport(new TestHttpClient(), accessTokenFactory, logger, true, TestWebSocket, headers || {});

    const connectPromise = webSocket.connect(url || "http://example.com", format || TransferFormat.Text);

    await TestWebSocket.webSocket.openSet;
    TestWebSocket.webSocket.onopen(new TestEvent());

    await connectPromise;

    return webSocket;
}
