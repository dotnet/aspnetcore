// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { TransferFormat } from "../src/ITransport";

import { HttpRequest, HttpResponse } from "../src/HttpClient";
import { ILogger } from "../src/ILogger";
import { ServerSentEventsTransport } from "../src/ServerSentEventsTransport";
import { getUserAgentHeader } from "../src/Utils";
import { VerifyLogger } from "./Common";
import { TestEventSource, TestMessageEvent } from "./TestEventSource";
import { TestHttpClient } from "./TestHttpClient";
import { registerUnhandledRejectionHandler } from "./Utils";
import { IHttpConnectionOptions } from "signalr/src/IHttpConnectionOptions";
import { AccessTokenHttpClient } from "../src/AccessTokenHttpClient";

registerUnhandledRejectionHandler();

describe("ServerSentEventsTransport", () => {
    it("does not allow non-text formats", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = new ServerSentEventsTransport(new TestHttpClient(), undefined, logger,
                { logMessageContent: true, EventSource: TestEventSource, withCredentials: true, headers: {} });

            await expect(sse.connect("", TransferFormat.Binary))
                .rejects
                .toEqual(new Error("The Server-Sent Events transport only supports the 'Text' transfer format"));
        });
    });

    it("connect waits for EventSource to be connected", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = new ServerSentEventsTransport(new TestHttpClient(), undefined, logger,
                { logMessageContent: true, EventSource: TestEventSource, withCredentials: true, headers: {} });

            let connectComplete: boolean = false;
            const connectPromise = (async () => {
                await sse.connect("http://example.com", TransferFormat.Text);
                connectComplete = true;
            })();

            await TestEventSource.eventSource.openSet;

            expect(connectComplete).toBe(false);

            TestEventSource.eventSource.onopen(new TestMessageEvent());

            await connectPromise;
            expect(connectComplete).toBe(true);
        });
    });

    it("connect failure does not call onclose handler", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = new ServerSentEventsTransport(new TestHttpClient(), undefined, logger,
                { logMessageContent: true, EventSource: TestEventSource, withCredentials: true, headers: {} });
            let closeCalled = false;
            sse.onclose = () => closeCalled = true;

            const connectPromise = (async () => {
                await sse.connect("http://example.com", TransferFormat.Text);
            })();

            await TestEventSource.eventSource.openSet;

            TestEventSource.eventSource.onerror(new TestMessageEvent());

            await expect(connectPromise)
                .rejects
                .toEqual(new Error("EventSource failed to connect. The connection could not be found on the server, either the connection ID is not present on the server, or a proxy is refusing/buffering the connection. If you have multiple servers check that sticky sessions are enabled."));
            expect(closeCalled).toBe(false);
        });
    });

    [["http://example.com", "http://example.com?access_token=secretToken"],
    ["http://example.com?value=null", "http://example.com?value=null&access_token=secretToken"]]
        .forEach(([input, expected]) => {
            it(`appends access_token to url ${input}`, async () => {
                await VerifyLogger.run(async (logger) => {
                    await createAndStartSSE(logger, input, () => "secretToken");

                    expect(TestEventSource.eventSource.url).toBe(expected);
                });
            });
        });

    it("sets Authorization header on sends", async () => {
        await VerifyLogger.run(async (logger) => {
            let request: HttpRequest;
            const httpClient = new AccessTokenHttpClient(new TestHttpClient().on((r) => {
                request = r;
                return "";
            }), () => "secretToken");

            const sse = await createAndStartSSE(logger, "http://example.com", () => "secretToken", { httpClient });

            await sse.send("");

            expect(request!.headers!.Authorization).toBe("Bearer secretToken");
            expect(request!.url).toBe("http://example.com");
        });
    });

    it("retries 401 requests on sends", async () => {
        await VerifyLogger.run(async (logger) => {
            let request: HttpRequest;
            let requestCount = 0;
            const httpClient = new AccessTokenHttpClient(new TestHttpClient().on((r) => {
                requestCount++;
                if (requestCount === 2) {
                    return new HttpResponse(401);
                }
                request = r;
                return "";
            }), () => "secretToken" + requestCount);

            // AccessTokenHttpClient assumes negotiate was called which would have called accessTokenFactory already
            // It also assumes the request shouldn't be retried if the factory was called, so we need to make a "negotiate" call
            // to test the retry behavior for send requests
            await httpClient.post("");
            expect(request!.headers!.Authorization).toBe("Bearer secretToken0");

            const sse = await createAndStartSSE(logger, "http://example.com", () => "secretToken", { httpClient });

            await sse.send("");

            expect(request!.headers!.Authorization).toBe("Bearer secretToken2");
            expect(request!.url).toBe("http://example.com");
            expect(requestCount).toEqual(3);
        });
    });

    it("can send data", async () => {
        await VerifyLogger.run(async (logger) => {
            let request: HttpRequest;
            const httpClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            const sse = await createAndStartSSE(logger, "http://example.com", undefined, { httpClient });

            await sse.send("send data");

            expect(request!.content).toBe("send data");
        });
    });

    it("can receive data", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = await createAndStartSSE(logger);

            let received: string | ArrayBuffer;
            sse.onreceive = (data) => {
                received = data;
            };

            const message = new TestMessageEvent();
            message.data = "receive data";
            TestEventSource.eventSource.onmessage(message);

            expect(typeof received!).toBe("string");
            expect(received!).toBe("receive data");
        });
    });

    it("stop closes EventSource and calls onclose", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = await createAndStartSSE(logger);

            let closeCalled: boolean = false;
            sse.onclose = () => {
                closeCalled = true;
            };

            await sse.stop();

            expect(closeCalled).toBe(true);
            expect(TestEventSource.eventSource.closed).toBe(true);
        });
    });

    it("can close from EventSource error", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = await createAndStartSSE(logger);

            let closeCalled: boolean = false;
            let error: Error | unknown;
            sse.onclose = (e) => {
                closeCalled = true;
                error = e;
            };

            const errorEvent = new TestMessageEvent();
            errorEvent.data = "error";
            TestEventSource.eventSource.onerror(errorEvent);

            expect(closeCalled).toBe(true);
            expect(TestEventSource.eventSource.closed).toBe(true);
            expect(error).toBeUndefined();
        });
    });

    it("send throws if not connected", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = new ServerSentEventsTransport(new TestHttpClient(), undefined, logger,
                { logMessageContent: true, EventSource: TestEventSource, withCredentials: true, headers: {} });

            await expect(sse.send(""))
                .rejects
                .toEqual(new Error("Cannot send until the transport is connected"));
        });
    });

    it("closes on error from receive", async () => {
        await VerifyLogger.run(async (logger) => {
            const sse = await createAndStartSSE(logger);

            sse.onreceive = () => {
                throw new Error("error parsing");
            };

            let closeCalled: boolean = false;
            let error: Error | unknown;
            sse.onclose = (e) => {
                closeCalled = true;
                error = e;
            };

            const errorEvent = new TestMessageEvent();
            errorEvent.data = "some data";
            TestEventSource.eventSource.onmessage(errorEvent);

            expect(closeCalled).toBe(true);
            expect(TestEventSource.eventSource.closed).toBe(true);
            expect(error).toEqual(new Error("error parsing"));
        });
    });

    it("sets user agent header on connect and sends", async () => {
        await VerifyLogger.run(async (logger) => {
            let request: HttpRequest;
            const httpClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            const sse = await createAndStartSSE(logger, "http://example.com", undefined, { httpClient });

            let [, value] = getUserAgentHeader();
            expect((TestEventSource.eventSource.eventSourceInitDict as any).headers[`User-Agent`]).toEqual(value);
            await sse.send("");

            [, value] = getUserAgentHeader();
            expect(request!.headers![`User-Agent`]).toBe(value);
            expect(request!.url).toBe("http://example.com");
        });
    });

    it("overwrites library headers with user headers", async () => {
        await VerifyLogger.run(async (logger) => {
            let request: HttpRequest;
            const httpClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            const headers = { "User-Agent": "Custom Agent", "X-HEADER": "VALUE" };
            const sse = await createAndStartSSE(logger, "http://example.com", undefined, { httpClient, headers });

            expect((TestEventSource.eventSource.eventSourceInitDict as any).headers["User-Agent"]).toEqual("Custom Agent");
            expect((TestEventSource.eventSource.eventSourceInitDict as any).headers["X-HEADER"]).toEqual("VALUE");
            await sse.send("");

            expect(request!.headers!["User-Agent"]).toEqual("Custom Agent");
            expect(request!.headers!["X-HEADER"]).toEqual("VALUE");
            expect(request!.url).toBe("http://example.com");
        });
    });

    it("sets timeout on sends", async () => {
        await VerifyLogger.run(async (logger) => {
            let request: HttpRequest;
            const httpClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            const sse = await createAndStartSSE(logger, "http://example.com", undefined, { httpClient, timeout: 123 });

            await sse.send("");

            expect(request!.timeout).toEqual(123);
        });
    });
});

async function createAndStartSSE(logger: ILogger, url?: string, accessTokenFactory?: (() => string | Promise<string>), options?: IHttpConnectionOptions): Promise<ServerSentEventsTransport> {
    let token;
    // SSE assumes (correctly) that negotiate will already create the token we want to use on connection startup, so simulate that here when creating the SSE transport
    if (accessTokenFactory) {
        token = await accessTokenFactory();
    }
    const sse = new ServerSentEventsTransport(options?.httpClient || new TestHttpClient(), token, logger,
        { logMessageContent: true, EventSource: TestEventSource, withCredentials: true, timeout: 10205, ...options });

    const connectPromise = sse.connect(url || "http://example.com", TransferFormat.Text);
    await TestEventSource.eventSource.openSet;

    TestEventSource.eventSource.onopen(new TestMessageEvent());
    await connectPromise;
    return sse;
}
