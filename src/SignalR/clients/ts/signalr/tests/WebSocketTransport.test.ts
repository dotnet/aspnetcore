// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ILogger } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
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
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket);

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
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket);

            let connectComplete: boolean = false;
            const connectPromise = (async () => {
                await webSocket.connect("http://example.com", TransferFormat.Text);
                connectComplete = true;
            })();

            await TestWebSocket.webSocket.closeSet;

            expect(connectComplete).toBe(false);

            TestWebSocket.webSocket.onerror(new TestEvent());

            await expect(connectPromise)
                .rejects
                .toThrow("There was an error with the transport.");
            expect(connectComplete).toBe(false);
        });
    });

    it("connect failure does not call onclose handler", async () => {
        await VerifyLogger.run(async (logger) => {
            (global as any).ErrorEvent = TestErrorEvent;
            const webSocket = new WebSocketTransport(new TestHttpClient(), undefined, logger, true, TestWebSocket);
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
                .toThrow("There was an error with the transport.");
            expect(connectComplete).toBe(false);
            expect(closeCalled).toBe(false);
        });
    });

    [["http://example.com", "ws://example.com?access_token=secretToken"],
    ["http://example.com?value=null", "ws://example.com?value=null&access_token=secretToken"],
    ["https://example.com?value=null", "wss://example.com?value=null&access_token=secretToken"]]
        .forEach(([input, expected]) => {
            it(`generates correct WebSocket URL for  ${input} with access_token`, async () => {
                await VerifyLogger.run(async (logger) => {
                    await createAndStartWebSocket(logger, input, () => "secretToken");

                    expect(TestWebSocket.webSocket.url).toBe(expected);
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
});

async function createAndStartWebSocket(logger: ILogger, url?: string, accessTokenFactory?: (() => string | Promise<string>), format?: TransferFormat): Promise<WebSocketTransport> {
    const webSocket = new WebSocketTransport(new TestHttpClient(), accessTokenFactory, logger, true, TestWebSocket);

    const connectPromise = webSocket.connect(url || "http://example.com", format || TransferFormat.Text);

    await TestWebSocket.webSocket.openSet;
    TestWebSocket.webSocket.onopen(new TestEvent());

    await connectPromise;

    return webSocket;
}
