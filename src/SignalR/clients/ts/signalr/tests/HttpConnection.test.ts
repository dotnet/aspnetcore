// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpResponse } from "../src/HttpClient";
import { HttpConnection, INegotiateResponse, TransportSendQueue } from "../src/HttpConnection";
import { IHttpConnectionOptions } from "../src/IHttpConnectionOptions";
import { HttpTransportType, ITransport, TransferFormat } from "../src/ITransport";
import { getUserAgentHeader } from "../src/Utils";

import { HttpError } from "../src/Errors";
import { ILogger, LogLevel } from "../src/ILogger";
import { NullLogger } from "../src/Loggers";
import { EventSourceConstructor, WebSocketConstructor } from "../src/Polyfills";

import { eachEndpointUrl, eachTransport, VerifyLogger } from "./Common";
import { TestHttpClient } from "./TestHttpClient";
import { TestTransport } from "./TestTransport";
import { TestEvent, TestWebSocket } from "./TestWebSocket";
import { PromiseSource, registerUnhandledRejectionHandler, SyncPoint } from "./Utils";

const commonOptions: IHttpConnectionOptions = {
    logger: NullLogger.instance,
};

const defaultConnectionId = "abc123";
const defaultConnectionToken = "123abc";
const defaultNegotiateResponse: INegotiateResponse = {
    availableTransports: [
        { transport: "WebSockets", transferFormats: ["Text", "Binary"] },
        { transport: "ServerSentEvents", transferFormats: ["Text"] },
        { transport: "LongPolling", transferFormats: ["Text", "Binary"] },
    ],
    connectionId: defaultConnectionId,
    connectionToken: defaultConnectionToken,
    negotiateVersion: 1,
};

registerUnhandledRejectionHandler();

describe("HttpConnection", () => {
    it("cannot be created with relative url if document object is not present", () => {
        expect(() => new HttpConnection("/test", commonOptions))
            .toThrow("Cannot resolve '/test'.");
    });

    it("cannot be created with relative url if window object is not present", () => {
        (global as any).window = {};
        expect(() => new HttpConnection("/test", commonOptions))
            .toThrow("Cannot resolve '/test'.");
        delete (global as any).window;
    });

    it("starting connection fails if getting id fails", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => Promise.reject("error"))
                    .on("GET", () => ""),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toBe("error");
        },
        "Failed to start the connection: error",
        "Failed to complete negotiation with the server: error");
    });

    it("cannot start a running connection", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => defaultNegotiateResponse),
                logger,
                transport: {
                    connect() {
                        return Promise.resolve();
                    },
                    send() {
                        return Promise.resolve();
                    },
                    stop() {
                        return Promise.resolve();
                    },
                    onclose: null,
                    onreceive: null,
                },
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);

                await expect(connection.start(TransferFormat.Text))
                    .rejects
                    .toThrow("Cannot start an HttpConnection that is not in the 'Disconnected' state.");
            } finally {
                (options.transport as ITransport).onclose!();
                await connection.stop();
            }
        });
    });

    it("can start a stopped connection", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => {
                        return Promise.reject("reached negotiate.");
                    })
                    .on("GET", () => ""),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toBe("reached negotiate.");

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toBe("reached negotiate.");
        },
        "Failed to complete negotiation with the server: reached negotiate.",
        "Failed to start the connection: reached negotiate.");
    });

    it("can stop a starting connection", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", async () => {
                        await connection.stop();
                        return "{}";
                    })
                    .on("GET", async () => {
                        await connection.stop();
                        return "";
                    }),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("The connection was stopped during negotiation.");
        },
        "Failed to start the connection: Error: The connection was stopped during negotiation.");
    });

    it("cannot send with an un-started connection", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new HttpConnection("http://tempuri.org");

            await expect(connection.send("LeBron James"))
                .rejects
                .toThrow("Cannot send data if the connection is not in the 'Connected' State.");
        });
    });

    it("sending before start doesn't throw synchronously", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new HttpConnection("http://tempuri.org");

            try {
                connection.send("test").catch((e) => {});
            } catch (e) {
                expect(false).toBe(true);
            }

        });
    });

    it("cannot be started if negotiate returns non 200 response", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => new HttpResponse(999))
                    .on("GET", () => ""),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Unexpected status code returned from negotiate '999'");
        },
        "Failed to start the connection: Error: Unexpected status code returned from negotiate '999'");
    });

    it("all transport failure errors get aggregated", async () => {
        await VerifyLogger.run(async (loggerImpl) => {
            let negotiateCount: number = 0;
            const options: IHttpConnectionOptions = {
                WebSocket: false,
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () =>  {
                        negotiateCount++;
                        return defaultNegotiateResponse;
                    })
                    .on("GET", () => new HttpResponse(200))
                    .on("DELETE", () => new HttpResponse(202)),

                logger: loggerImpl,
                transport: HttpTransportType.WebSockets,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Unable to connect to the server with any of the available transports. WebSockets failed: Error: There was an error with the transport. " +
                "ServerSentEvents failed: Error: 'ServerSentEvents' is disabled by the client. LongPolling failed: Error: 'LongPolling' is disabled by the client.");

            expect(negotiateCount).toEqual(1);
        },
        "Failed to start the transport 'WebSockets': Error: There was an error with the transport.",
        "Failed to start the connection: Error: Unable to connect to the server with any of the available transports. WebSockets failed: Error: There was an error with the transport. " +
        "ServerSentEvents failed: Error: 'ServerSentEvents' is disabled by the client. LongPolling failed: Error: 'LongPolling' is disabled by the client.");
    });

    it("negotiate called again when transport fails to start and falls back", async () => {
        await VerifyLogger.run(async (loggerImpl) => {
            let negotiateCount: number = 0;
            const options: IHttpConnectionOptions = {
                EventSource: () => { throw new Error("Don't allow ServerSentEvents."); },
                WebSocket: () => { throw new Error("Don't allow Websockets."); },
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () =>  {
                        negotiateCount++;
                        return defaultNegotiateResponse;
                    })
                    .on("GET", () => new HttpResponse(200))
                    .on("DELETE", () => new HttpResponse(202)),

                logger: loggerImpl,
                transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Unable to connect to the server with any of the available transports. WebSockets failed: Error: Don't allow Websockets. ServerSentEvents failed: Error: Don't allow ServerSentEvents. LongPolling failed: Error: 'LongPolling' is disabled by the client.");

            expect(negotiateCount).toEqual(2);
        },
        "Failed to start the transport 'WebSockets': Error: Don't allow Websockets.",
        "Failed to start the transport 'ServerSentEvents': Error: Don't allow ServerSentEvents.",
        "Failed to start the connection: Error: Unable to connect to the server with any of the available transports. WebSockets failed: Error: Don't allow Websockets. ServerSentEvents failed: Error: Don't allow ServerSentEvents. LongPolling failed: Error: 'LongPolling' is disabled by the client.");
    });

    it("failed re-negotiate fails start", async () => {
        await VerifyLogger.run(async (loggerImpl) => {
            let negotiateCount: number = 0;
            const options: IHttpConnectionOptions = {
                EventSource: () => { throw new Error("Don't allow ServerSentEvents."); },
                WebSocket: () => { throw new Error("Don't allow Websockets."); },
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () =>  {
                        negotiateCount++;
                        if (negotiateCount === 2) {
                            throw new Error("negotiate failed");
                        }
                        return defaultNegotiateResponse;
                    })
                    .on("GET", () => new HttpResponse(200))
                    .on("DELETE", () => new HttpResponse(202)),

                logger: loggerImpl,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("negotiate failed");

            expect(negotiateCount).toEqual(2);
        },
        "Failed to start the transport 'WebSockets': Error: Don't allow Websockets.",
        "Failed to complete negotiation with the server: Error: negotiate failed",
        "Failed to start the connection: Error: negotiate failed");
    });

    it("can stop a non-started connection", async () => {
        await VerifyLogger.run(async (logger) => {
            const connection = new HttpConnection("http://tempuri.org", { ...commonOptions, logger });
            await connection.stop();
        });
    });

    it("start throws after all transports fail", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [] }))
                    .on("GET", () => { throw new Error("fail"); }),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org?q=myData", options);

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("None of the transports supported by the client are supported by the server.");
        },
        "Failed to start the connection: Error: None of the transports supported by the client are supported by the server.");
    });

    it("preserves user's query string", async () => {
        await VerifyLogger.run(async (logger) => {
            const connectUrl = new PromiseSource<string>();
            const fakeTransport: ITransport = {
                connect(url: string): Promise<void> {
                    connectUrl.resolve(url);
                    return Promise.resolve();
                },
                send(): Promise<void> {
                    return Promise.resolve();
                },
                stop(): Promise<void> {
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
            };

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => "{ \"connectionId\": \"42\" }")
                    .on("GET", () => ""),
                logger,
                transport: fakeTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org?q=myData", options);
            try {
                const startPromise = connection.start(TransferFormat.Text);

                expect(await connectUrl).toBe("http://tempuri.org?q=myData&id=42");

                await startPromise;
            } finally {
                (options.transport as ITransport).onclose!();
                await connection.stop();
            }
        });
    });

    eachEndpointUrl((givenUrl: string, expectedUrl: string) => {
        it(`negotiate request for '${givenUrl}' puts 'negotiate' at the end of the path`, async () => {
            await VerifyLogger.run(async (logger) => {
                const negotiateUrl = new PromiseSource<string>();
                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    httpClient: new TestHttpClient()
                        .on("POST", (r) => {
                            negotiateUrl.resolve(r.url);
                            throw new HttpError("We don't care how this turns out", 500);
                        })
                        .on("GET", () => {
                            return new HttpResponse(204);
                        })
                        .on("DELETE", () => new HttpResponse(202)),
                    logger,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection(givenUrl, options);
                try {
                    const startPromise = connection.start(TransferFormat.Text);

                    expect(await negotiateUrl).toBe(expectedUrl);

                    await expect(startPromise).rejects.toThrow("We don't care how this turns out");
                } finally {
                    await connection.stop();
                }
            },
            "Failed to complete negotiation with the server: Error: We don't care how this turns out",
            "Failed to start the connection: Error: We don't care how this turns out");
        });
    });

    eachTransport((requestedTransport: HttpTransportType) => {
        it(`cannot be started if requested ${HttpTransportType[requestedTransport]} transport not available on server`, async () => {
            await VerifyLogger.run(async (logger) => {
                // Clone the default response
                const negotiateResponse = { ...defaultNegotiateResponse };

                // Remove the requested transport from the response
                negotiateResponse.availableTransports = negotiateResponse.availableTransports!
                    .filter((f) => f.transport !== HttpTransportType[requestedTransport]);

                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    httpClient: new TestHttpClient()
                        .on("POST", () => negotiateResponse)
                        .on("GET", () => new HttpResponse(204)),
                    logger,
                    transport: requestedTransport,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);

                await expect(connection.start(TransferFormat.Text))
                    .rejects
                    .toThrow(`Unable to connect to the server with any of the available transports. ${negotiateResponse.availableTransports[0].transport} failed: Error: '${negotiateResponse.availableTransports[0].transport}' is disabled by the client.` +
                    ` ${negotiateResponse.availableTransports[1].transport} failed: Error: '${negotiateResponse.availableTransports[1].transport}' is disabled by the client.`);
            },
            /Failed to start the connection: Error: Unable to connect to the server with any of the available transports. [a-zA-Z]+\b failed: Error: '[a-zA-Z]+\b' is disabled by the client. [a-zA-Z]+\b failed: Error: '[a-zA-Z]+\b' is disabled by the client./);
        });

        it(`cannot be started if server's only transport (${HttpTransportType[requestedTransport]}) is masked out by the transport option`, async () => {
            await VerifyLogger.run(async (logger) => {
                const negotiateResponse = {
                    availableTransports: [
                        { transport: "WebSockets", transferFormats: ["Text", "Binary"] },
                        { transport: "ServerSentEvents", transferFormats: ["Text"] },
                        { transport: "LongPolling", transferFormats: ["Text", "Binary"] },
                    ],
                    connectionId: "abc123",
                };

                // Build the mask by inverting the requested transport
                const transportMask = ~requestedTransport;

                // Remove all transports other than the requested one
                negotiateResponse.availableTransports = negotiateResponse.availableTransports
                    .filter((r) => r.transport === HttpTransportType[requestedTransport]);

                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    httpClient: new TestHttpClient()
                        .on("POST", () => negotiateResponse)
                        .on("GET", () => new HttpResponse(204)),
                    logger,
                    transport: transportMask,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);

                try {
                    await connection.start(TransferFormat.Text);
                    fail("Expected connection.start to throw!");
                } catch (e) {
                    expect(e.message).toBe(`Unable to connect to the server with any of the available transports. ${HttpTransportType[requestedTransport]} failed: Error: '${HttpTransportType[requestedTransport]}' is disabled by the client.`);
                }
            },
            `Failed to start the connection: Error: Unable to connect to the server with any of the available transports. ${HttpTransportType[requestedTransport]} failed: Error: '${HttpTransportType[requestedTransport]}' is disabled by the client.`);
        });
    });

    for (const [val, name] of [[null, "null"], [undefined, "undefined"], [0, "0"]]) {
        it(`can be started when transport mask is ${name}`, async () => {
            let websocketOpen: (() => any) | null = null;
            const sync: SyncPoint = new SyncPoint();
            const websocket = class WebSocket {
                constructor() {
                    this._onopen = null;
                }
                // tslint:disable-next-line:variable-name
                private _onopen: ((this: WebSocket, ev: Event) => any) | null;
                public get onopen(): ((this: WebSocket, ev: Event) => any) | null {
                    return this._onopen;
                }
                public set onopen(onopen: ((this: WebSocket, ev: Event) => any) | null) {
                    this._onopen = onopen;
                    websocketOpen = () => this._onopen!({} as Event);
                    sync.continue();
                }

                public close(): void {
                }
            };

            await VerifyLogger.run(async (logger) => {
                const options: IHttpConnectionOptions = {
                    WebSocket: websocket as any,
                    ...commonOptions,
                    httpClient: new TestHttpClient()
                        .on("POST", () => defaultNegotiateResponse)
                        .on("GET", () => new HttpResponse(200))
                        .on("DELETE", () => new HttpResponse(202)),
                    logger,
                    transport: val,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);

                const startPromise = connection.start(TransferFormat.Text);
                await sync.waitToContinue();
                websocketOpen!();
                await startPromise;

                await connection.stop();
            });
        });
    }

    it("cannot be started if no transport available on server and no transport requested", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [] }))
                    .on("GET", () => ""),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("None of the transports supported by the client are supported by the server.");
        },
        "Failed to start the connection: Error: None of the transports supported by the client are supported by the server.");
    });

    it("does not send negotiate request if WebSockets transport requested explicitly and skipNegotiation is true", async () => {
        const websocket = class WebSocket {
            constructor() {
                throw new Error("WebSocket constructor called.");
            }
        };
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                WebSocket: websocket as any,
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => { throw new Error("Should not be called"); })
                    .on("GET", () => { throw new Error("Should not be called"); }),
                logger,
                skipNegotiation: true,
                transport: HttpTransportType.WebSockets,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("WebSocket constructor called.");
        },
        "Failed to start the connection: Error: WebSocket constructor called.");
    });

    it("does not start non WebSockets transport if requested explicitly and skipNegotiation is true", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient(),
                logger,
                skipNegotiation: true,
                transport: HttpTransportType.LongPolling,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Negotiation can only be skipped when using the WebSocket transport directly.");
        },
        "Failed to start the connection: Error: Negotiation can only be skipped when using the WebSocket transport directly.");
    });

    it("redirects to url when negotiate returns it", async () => {
        await VerifyLogger.run(async (logger) => {
            let firstNegotiate = true;
            let firstPoll = true;
            const httpClient = new TestHttpClient()
                .on("POST", /\/negotiate/, () => {
                    if (firstNegotiate) {
                        firstNegotiate = false;
                        return { url: "https://another.domain.url/chat" };
                    }
                    return {
                        availableTransports: [{ transport: "LongPolling", transferFormats: ["Text"] }],
                        connectionId: "0rge0d00-0040-0030-0r00-000q00r00e00",
                    };
                })
                .on("GET", () => {
                    if (firstPoll) {
                        firstPoll = false;
                        return "";
                    }
                    return new HttpResponse(204, "No Content", "");
                })
                .on("DELETE", () => new HttpResponse(202));

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient,
                logger,
                transport: HttpTransportType.LongPolling,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);

                expect(httpClient.sentRequests.length).toBe(4);
                expect(httpClient.sentRequests[0].url).toBe("http://tempuri.org/negotiate?negotiateVersion=1");
                expect(httpClient.sentRequests[1].url).toBe("https://another.domain.url/chat/negotiate?negotiateVersion=1");
                expect(httpClient.sentRequests[2].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
                expect(httpClient.sentRequests[3].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
            } finally {
                await connection.stop();
            }
        });
    });

    it("fails to start if negotiate redirects more than 100 times", async () => {
        await VerifyLogger.run(async (logger) => {
            const httpClient = new TestHttpClient()
                .on("POST", /\/negotiate/, () => ({ url: "https://another.domain.url/chat" }));

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient,
                logger,
                transport: HttpTransportType.LongPolling,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Negotiate redirection limit exceeded.");
        },
        "Failed to start the connection: Error: Negotiate redirection limit exceeded.");
    });

    it("redirects to url when negotiate returns it with access token", async () => {
        await VerifyLogger.run(async (logger) => {
            let firstNegotiate = true;
            let firstPoll = true;
            const httpClient = new TestHttpClient()
                .on("POST", /\/negotiate/, (r) => {
                    if (firstNegotiate) {
                        firstNegotiate = false;

                        if (r.headers && r.headers.Authorization !== "Bearer firstSecret") {
                            return new HttpResponse(401, "Unauthorized", "");
                        }

                        return { url: "https://another.domain.url/chat", accessToken: "secondSecret" };
                    }

                    if (r.headers && r.headers.Authorization !== "Bearer secondSecret") {
                        return new HttpResponse(401, "Unauthorized", "");
                    }

                    return {
                        availableTransports: [{ transport: "LongPolling", transferFormats: ["Text"] }],
                        connectionId: "0rge0d00-0040-0030-0r00-000q00r00e00",
                    };
                })
                .on("GET", (r) => {
                    if (r.headers && r.headers.Authorization !== "Bearer secondSecret") {
                        return new HttpResponse(401, "Unauthorized", "");
                    }

                    if (firstPoll) {
                        firstPoll = false;
                        return "";
                    }
                    return new HttpResponse(204, "No Content", "");
                })
                .on("DELETE", () => new HttpResponse(202));

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                accessTokenFactory: () => "firstSecret",
                httpClient,
                logger,
                transport: HttpTransportType.LongPolling,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);

                expect(httpClient.sentRequests.length).toBe(4);
                expect(httpClient.sentRequests[0].url).toBe("http://tempuri.org/negotiate?negotiateVersion=1");
                expect(httpClient.sentRequests[1].url).toBe("https://another.domain.url/chat/negotiate?negotiateVersion=1");
                expect(httpClient.sentRequests[2].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
                expect(httpClient.sentRequests[3].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
            } finally {
                await connection.stop();
            }
        });
    });

    it("throws error if negotiate response has error", async () => {
        await VerifyLogger.run(async (logger) => {
            const httpClient = new TestHttpClient()
                .on("POST", /\/negotiate/, () => ({ error: "Negotiate error." }));

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient,
                logger,
                transport: HttpTransportType.LongPolling,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Negotiate error.");
        },
        "Failed to start the connection: Error: Negotiate error.");
    });

    it("authorization header removed when token factory returns null and using LongPolling", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "LongPolling", transferFormats: ["Text"] };

            let httpClientGetCount = 0;
            let accessTokenFactoryCount = 0;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                accessTokenFactory: () => {
                    accessTokenFactoryCount++;
                    if (accessTokenFactoryCount === 1) {
                        return "A token value";
                    } else {
                        // Return a null value after the first call to test the header being removed
                        return null;
                    }
                },
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [availableTransport] }))
                    .on("GET", (r) => {
                        httpClientGetCount++;
                        // tslint:disable-next-line:no-string-literal
                        const authorizationValue = r.headers!["Authorization"];
                        if (httpClientGetCount === 1) {
                            if (authorizationValue) {
                                fail("First long poll request should have a authorization header.");
                            }
                            // First long polling request must succeed so start completes
                            return "";
                        } else {
                            // Check second long polling request has its header removed
                            if (authorizationValue) {
                                fail("Second long poll request should have no authorization header.");
                            }
                        }
                    })
                    .on("DELETE", () => new HttpResponse(202)),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);

                expect(httpClientGetCount).toBeGreaterThanOrEqual(2);
                expect(accessTokenFactoryCount).toBeGreaterThanOrEqual(2);
            } finally {
                await connection.stop();
            }
        });
    });

    it("sets inherentKeepAlive feature when using LongPolling", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "LongPolling", transferFormats: ["Text"] };

            let httpClientGetCount = 0;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [availableTransport] }))
                    .on("GET", () => {
                        httpClientGetCount++;
                        if (httpClientGetCount === 1) {
                            // First long polling request must succeed so start completes
                            return "";
                        }
                    })
                    .on("DELETE", () => new HttpResponse(202)),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);
                expect(connection.features.inherentKeepAlive).toBe(true);
            } finally {
                await connection.stop();
            }
        });
    });

    it("transport handlers set before start", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "LongPolling", transferFormats: ["Text"] };
            let handlersSet = false;

            let httpClientGetCount = 0;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [availableTransport] }))
                    .on("GET", () => {
                        httpClientGetCount++;
                        if (httpClientGetCount === 1) {
                            if ((connection as any).transport.onreceive && (connection as any).transport.onclose) {
                                handlersSet = true;
                            }
                            // First long polling request must succeed so start completes
                            return "";
                        }
                    })
                    .on("DELETE", () => new HttpResponse(202)),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            connection.onreceive = () => null;
            try {
                await connection.start(TransferFormat.Text);
            } finally {
                await connection.stop();
            }

            expect(handlersSet).toBe(true);
        });
    });

    it("transport handlers set before start for custom transports", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "Custom", transferFormats: ["Text"] };
            let handlersSet = false;
            const transport: ITransport = {
                connect: (url: string, transferFormat: TransferFormat) => {
                    if (transport.onreceive && transport.onclose) {
                        handlersSet = true;
                    }
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
                send: (data: any) => Promise.resolve(),
                stop: () => {
                    if (transport.onclose) {
                        transport.onclose();
                    }
                    return Promise.resolve();
                },
            };

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [availableTransport] })),
                logger,
                transport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            connection.onreceive = () => null;
            try {
                await connection.start(TransferFormat.Text);
            } finally {
                await connection.stop();
            }

            expect(handlersSet).toBe(true);
        });
    });

    it("missing negotiateVersion ignores connectionToken", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "Custom", transferFormats: ["Text"] };
            const transport = {
                connect(url: string, transferFormat: TransferFormat) {
                    return Promise.resolve();
                },
                send(data: any) {
                    return Promise.resolve();
                },
                stop() {
                    if (transport.onclose) {
                        transport.onclose();
                    }
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
            } as ITransport;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", connectionToken: "token", availableTransports: [availableTransport] })),
                logger,
                transport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            connection.onreceive = () => null;
            try {
                await connection.start(TransferFormat.Text);
                expect(connection.connectionId).toBe("42");
            } finally {
                await connection.stop();
            }
        });
    });

    it("negotiate version 0 ignores connectionToken", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "Custom", transferFormats: ["Text"] };
            const transport = {
                connect(url: string, transferFormat: TransferFormat) {
                    return Promise.resolve();
                },
                send(data: any) {
                    return Promise.resolve();
                },
                stop() {
                    if (transport.onclose) {
                        transport.onclose();
                    }
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
            } as ITransport;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", connectionToken: "token", negotiateVersion: 0, availableTransports: [availableTransport] })),
                logger,
                transport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            connection.onreceive = () => null;
            try {
                await connection.start(TransferFormat.Text);
                expect(connection.connectionId).toBe("42");
            } finally {
                await connection.stop();
            }
        });
    });

    it("negotiate version 1 uses connectionToken for url and connectionId for property", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "Custom", transferFormats: ["Text"] };
            let connectUrl = "";
            const transport = {
                connect(url: string, transferFormat: TransferFormat) {
                    connectUrl = url;
                    return Promise.resolve();
                },
                send(data: any) {
                    return Promise.resolve();
                },
                stop() {
                    if (transport.onclose) {
                        transport.onclose();
                    }
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
            } as ITransport;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", connectionToken: "token", negotiateVersion: 1, availableTransports: [availableTransport] })),
                logger,
                transport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            connection.onreceive = () => null;
            try {
                await connection.start(TransferFormat.Text);
                expect(connection.connectionId).toBe("42");
                expect(connectUrl).toBe("http://tempuri.org?id=token");
            } finally {
                await connection.stop();
            }
        });
    });

    it("negotiateVersion query string not added if already present", async () => {
        await VerifyLogger.run(async (logger) => {
            const connectUrl = new PromiseSource<string>();
            const fakeTransport: ITransport = {
                connect(url: string): Promise<void> {
                    connectUrl.resolve(url);
                    return Promise.resolve();
                },
                send(): Promise<void> {
                    return Promise.resolve();
                },
                stop(): Promise<void> {
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
            };

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", "http://tempuri.org/negotiate?negotiateVersion=42", () => "{ \"connectionId\": \"42\" }")
                    .on("GET", () => ""),
                logger,
                transport: fakeTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org?negotiateVersion=42", options);
            try {
                const startPromise = connection.start(TransferFormat.Text);

                expect(await connectUrl).toBe("http://tempuri.org?negotiateVersion=42&id=42");

                await startPromise;
            } finally {
                (options.transport as ITransport).onclose!();
                await connection.stop();
            }
        });
    });

    it("negotiateVersion query string not added if already present after redirect", async () => {
        await VerifyLogger.run(async (logger) => {
            const connectUrl = new PromiseSource<string>();
            const fakeTransport: ITransport = {
                connect(url: string): Promise<void> {
                    connectUrl.resolve(url);
                    return Promise.resolve();
                },
                send(): Promise<void> {
                    return Promise.resolve();
                },
                stop(): Promise<void> {
                    return Promise.resolve();
                },
                onclose: null,
                onreceive: null,
            };

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", "http://tempuri.org/negotiate?negotiateVersion=1", () => "{ \"url\": \"http://redirect.org\" }")
                    .on("POST", "http://redirect.org/negotiate?negotiateVersion=1", () => "{ \"connectionId\": \"42\"}")
                    .on("GET", () => ""),
                logger,
                transport: fakeTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                const startPromise = connection.start(TransferFormat.Text);

                expect(await connectUrl).toBe("http://redirect.org?id=42");

                await startPromise;
            } finally {
                (options.transport as ITransport).onclose!();
                await connection.stop();
            }
        });
    });

    it("fallback changes connectionId property", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransports = [{ transport: "WebSockets", transferFormats: ["Text"] }, { transport: "LongPolling", transferFormats: ["Text"] }];
            let negotiateCount: number = 0;
            let getCount: number = 0;
            let connection: HttpConnection;
            let connectionId: string | undefined;
            const options: IHttpConnectionOptions = {
                WebSocket: TestWebSocket,
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () =>  {
                        negotiateCount++;
                        return ({ connectionId: negotiateCount.toString(), connectionToken: "token", negotiateVersion: 1, availableTransports });
                    })
                    .on("GET", () => {
                        getCount++;
                        if (getCount === 1) {
                            return new HttpResponse(200);
                        }
                        connectionId = connection.connectionId;
                        return new HttpResponse(204);
                    })
                    .on("DELETE", () => new HttpResponse(202)),

                logger,
            } as IHttpConnectionOptions;

            TestWebSocket.webSocketSet = new PromiseSource();

            connection = new HttpConnection("http://tempuri.org", options);
            const startPromise = connection.start(TransferFormat.Text);

            await TestWebSocket.webSocketSet;
            await TestWebSocket.webSocket.closeSet;
            TestWebSocket.webSocket.onerror(new TestEvent());

            try {
                await startPromise;
            } catch { }

            expect(negotiateCount).toEqual(2);
            expect(connectionId).toEqual("2");
        },
        "Failed to start the transport 'WebSockets': Error: There was an error with the transport.");
    });

    it("user agent header set on negotiate", async () => {
        await VerifyLogger.run(async (logger) => {
            let userAgentValue: string = "";
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => {
                        userAgentValue = r.headers![`User-Agent`];
                        return new HttpResponse(200, "", "{\"error\":\"nope\"}");
                    }),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);
            } catch {
            } finally {
                await connection.stop();
            }

            const [, value] = getUserAgentHeader();
            expect(userAgentValue).toEqual(value);
        }, "Failed to start the connection: Error: nope");
    });

    it("logMessageContent displays correctly with binary data", async () => {
        await VerifyLogger.run(async (logger) => {
            const availableTransport = { transport: "LongPolling", transferFormats: ["Text", "Binary"] };

            let sentMessage = "";
            const captureLogger: ILogger = {
                log: (logLevel: LogLevel, message: string) => {
                    if (logLevel === LogLevel.Trace && message.search("data of length") > 0) {
                        sentMessage = message;
                    }

                    logger.log(logLevel, message);
                },
            };

            let httpClientGetCount = 0;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", () => ({ connectionId: "42", availableTransports: [availableTransport] }))
                    .on("GET", () => {
                        httpClientGetCount++;
                        if (httpClientGetCount === 1) {
                            // First long polling request must succeed so start completes
                            return "";
                        }
                        return Promise.resolve();
                    })
                    .on("DELETE", () => new HttpResponse(202)),
                logMessageContent: true,
                logger: captureLogger,
                transport: HttpTransportType.LongPolling,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            connection.onreceive = () => null;
            try {
                await connection.start(TransferFormat.Binary);
                await connection.send(new Uint8Array([0x68, 0x69, 0x20, 0x3a, 0x29]));
            } finally {
                await connection.stop();
            }

            expect(sentMessage).toBe("(LongPolling transport) sending data. Binary data of length 5. Content: '0x68 0x69 0x20 0x3a 0x29'.");
        });
    });

    it("send after restarting connection works", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                WebSocket: TestWebSocket,
                httpClient: new TestHttpClient()
                    .on("POST", () => defaultNegotiateResponse)
                    .on("GET", () => ""),
                logger,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            const closePromise = new PromiseSource();
            connection.onclose = (e) => {
                closePromise.resolve();
            };

            TestWebSocket.webSocketSet = new PromiseSource();
            let startPromise = connection.start(TransferFormat.Text);
            await TestWebSocket.webSocketSet;
            await TestWebSocket.webSocket.openSet;
            TestWebSocket.webSocket.onopen(new TestEvent());
            await startPromise;

            await connection.send("text");
            TestWebSocket.webSocket.close();
            TestWebSocket.webSocketSet = new PromiseSource();

            await closePromise;

            startPromise = connection.start(TransferFormat.Text);
            await TestWebSocket.webSocketSet;
            TestWebSocket.webSocket.onopen(new TestEvent());
            await startPromise;
            await connection.send("text");
        });
    });

    describe(".constructor", () => {
        it("throws if no Url is provided", async () => {
            // Force TypeScript to let us call the constructor incorrectly :)
            expect(() => new (HttpConnection as any)()).toThrowError("The 'url' argument is required.");
        });

        it("uses EventSource constructor from options if provided", async () => {
            await VerifyLogger.run(async (logger) => {
                let eventSourceConstructorCalled: boolean = false;

                class TestEventSource {
                    // The "_" prefix tell TypeScript not to worry about unused parameter, but tslint doesn't like it.
                    // tslint:disable-next-line:variable-name
                    constructor(_url: string, _eventSourceInitDict: EventSourceInit) {
                        eventSourceConstructorCalled = true;
                        throw new Error("EventSource constructor called.");
                    }
                }

                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    EventSource: TestEventSource as EventSourceConstructor,
                    httpClient: new TestHttpClient().on("POST", () => {
                        return {
                            availableTransports: [
                                { transport: "ServerSentEvents", transferFormats: ["Text"] },
                            ],
                            connectionId: defaultConnectionId,
                        };
                    }),
                    logger,
                    transport: HttpTransportType.ServerSentEvents,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);

                await expect(connection.start(TransferFormat.Text))
                    .rejects
                    .toEqual(new Error("Unable to connect to the server with any of the available transports. ServerSentEvents failed: Error: EventSource constructor called."));

                expect(eventSourceConstructorCalled).toEqual(true);
            },
            "Failed to start the transport 'ServerSentEvents': Error: EventSource constructor called.",
            "Failed to start the connection: Error: Unable to connect to the server with any of the available transports. ServerSentEvents failed: Error: EventSource constructor called.");
        });

        it("uses WebSocket constructor from options if provided", async () => {
            await VerifyLogger.run(async (logger) => {
                class BadConstructorWebSocket {
                    // The "_" prefix tell TypeScript not to worry about unused parameter, but tslint doesn't like it.
                    // tslint:disable-next-line:variable-name
                    constructor(_url: string, _protocols?: string | string[]) {
                        throw new Error("WebSocket constructor called.");
                    }
                }

                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    WebSocket: BadConstructorWebSocket as WebSocketConstructor,
                    logger,
                    skipNegotiation: true,
                    transport: HttpTransportType.WebSockets,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);

                await expect(connection.start())
                    .rejects
                    .toThrow("WebSocket constructor called.");
            },
            "Failed to start the connection: Error: WebSocket constructor called.");
        });
    });

    describe("startAsync", () => {
        it("throws if an unsupported TransferFormat is provided", async () => {
            await VerifyLogger.run(async (logger) => {
                // Force TypeScript to let us call start incorrectly
                const connection: any = new HttpConnection("http://tempuri.org", { ...commonOptions, logger });

                await expect(connection.start(42)).rejects.toThrow("Unknown transferFormat value: 42.");
            });
        });

        it("throws if trying to connect to an ASP.NET SignalR Server", async () => {
            await VerifyLogger.run(async (logger) => {
                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    httpClient: new TestHttpClient()
                        .on("POST", () => "{\"Url\":\"/signalr\"," +
                            "\"ConnectionToken\":\"X97dw3uxW4NPPggQsYVcNcyQcuz4w2\"," +
                            "\"ConnectionId\":\"05265228-1e2c-46c5-82a1-6a5bcc3f0143\"," +
                            "\"KeepAliveTimeout\":10.0," +
                            "\"DisconnectTimeout\":5.0," +
                            "\"TryWebSockets\":true," +
                            "\"ProtocolVersion\":\"1.5\"," +
                            "\"TransportConnectTimeout\":30.0," +
                            "\"LongPollDelay\":0.0}")
                        .on("GET", () => ""),
                    logger,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);
                let receivedError = false;
                try {
                    await connection.start(TransferFormat.Text);
                } catch (error) {
                    expect(error).toEqual(new Error("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details."));
                    receivedError = true;
                } finally {
                    await connection.stop();
                }
                expect(receivedError).toBe(true);
            },
            "Failed to start the connection: Error: Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.");
        });
    });
});

describe("TransportSendQueue", () => {
    it("sends data when not currently sending", async () => {
        const sendMock = jest.fn(() => Promise.resolve());
        const transport = new TestTransport();
        transport.send = sendMock;
        const queue = new TransportSendQueue(transport);

        const x = queue.send("Hello");
        await x;

        expect(sendMock.mock.calls.length).toBe(1);

        const stop = queue.stop();
        await stop;
    });

    it("sends buffered data on fail", async () => {
        const promiseSource1 = new PromiseSource();
        const promiseSource2 = new PromiseSource();
        const promiseSource3 = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource1;
                promiseSource2.resolve();
                await promiseSource3;
            })
            .mockImplementationOnce(() => Promise.resolve());
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        const first = queue.send("Hello");
        // This should allow first to enter transport.send
        promiseSource1.resolve();
        // Wait until we're inside transport.send
        await promiseSource2;

        // This should get queued.
        const second = queue.send("world");

        promiseSource3.reject("Test error");
        await expect(first).rejects.toBe("Test error");

        await second;

        expect(sendMock.mock.calls.length).toBe(2);
        expect(sendMock.mock.calls[0][0]).toEqual("Hello");
        expect(sendMock.mock.calls[1][0]).toEqual("world");

        await queue.stop();
    });

    it("rejects promise for buffered sends", async () => {
        const promiseSource1 = new PromiseSource();
        const promiseSource2 = new PromiseSource();
        const promiseSource3 = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource1;
                promiseSource2.resolve();
                await promiseSource3;
            })
            .mockImplementationOnce(() => Promise.reject("Test error"));
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        const first = queue.send("Hello");
        // This should allow first to enter transport.send
        promiseSource1.resolve();
        // Wait until we're inside transport.send
        await promiseSource2;

        // This should get queued.
        const second = queue.send("world");

        promiseSource3.resolve();

        await first;
        await expect(second).rejects.toBeDefined();

        expect(sendMock.mock.calls.length).toBe(2);
        expect(sendMock.mock.calls[0][0]).toEqual("Hello");
        expect(sendMock.mock.calls[1][0]).toEqual("world");

        await queue.stop();
    });

    it ("concatenates string sends", async () => {
        const promiseSource1 = new PromiseSource();
        const promiseSource2 = new PromiseSource();
        const promiseSource3 = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource1;
                promiseSource2.resolve();
                await promiseSource3;
            })
            .mockImplementationOnce(() => Promise.resolve());
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        const first = queue.send("Hello");
        // This should allow first to enter transport.send
        promiseSource1.resolve();
        // Wait until we're inside transport.send
        await promiseSource2;

        // These two operations should get queued.
        const second = queue.send("world");
        const third = queue.send("!");

        promiseSource3.resolve();

        await Promise.all([first, second, third]);

        expect(sendMock.mock.calls.length).toBe(2);
        expect(sendMock.mock.calls[0][0]).toEqual("Hello");
        expect(sendMock.mock.calls[1][0]).toEqual("world!");

        await queue.stop();
    });

    it ("concatenates buffered ArrayBuffer", async () => {
        const promiseSource1 = new PromiseSource();
        const promiseSource2 = new PromiseSource();
        const promiseSource3 = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource1;
                promiseSource2.resolve();
                await promiseSource3;
            })
            .mockImplementationOnce(() => Promise.resolve());
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        const first = queue.send(new Uint8Array([4, 5, 6]).buffer);
        // This should allow first to enter transport.send
        promiseSource1.resolve();
        // Wait until we're inside transport.send
        await promiseSource2;

        // These two operations should get queued.
        const second = queue.send(new Uint8Array([7, 8, 10]));
        const third = queue.send(new Uint8Array([12, 14]));

        promiseSource3.resolve();

        await Promise.all([first, second, third]);

        expect(sendMock.mock.calls.length).toBe(2);
        expect(sendMock.mock.calls[0][0]).toEqual(new Uint8Array([4, 5, 6]).buffer);
        expect(sendMock.mock.calls[1][0]).toEqual(new Uint8Array([7, 8, 10, 12, 14]).buffer);

        await queue.stop();
    });

    it ("throws if mixed data is queued", async () => {
        const promiseSource1 = new PromiseSource();
        const promiseSource2 = new PromiseSource();
        const promiseSource3 = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource1;
                promiseSource2.resolve();
                await promiseSource3;
            })
            .mockImplementationOnce(() => Promise.resolve());
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        const first = queue.send(new Uint8Array([4, 5, 6]));
        // This should allow first to enter transport.send
        promiseSource1.resolve();
        // Wait until we're inside transport.send
        await promiseSource2;

        // These two operations should get queued.
        const second = queue.send(new Uint8Array([7, 8, 10]));
        expect(() => queue.send("A string!")).toThrow();

        promiseSource3.resolve();

        await Promise.all([first, second]);
        await queue.stop();
    });

    it ("rejects pending promises on stop", async () => {
        const promiseSource = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource;
            });
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        const send = queue.send("Test");
        await queue.stop();

        await expect(send).rejects.toBe("Connection stopped.");
    });

    it ("prevents additional sends after stop", async () => {
        const promiseSource = new PromiseSource();
        const transport = new TestTransport();
        const sendMock = jest.fn()
            .mockImplementationOnce(async () => {
                await promiseSource;
            });
        transport.send = sendMock;

        const queue = new TransportSendQueue(transport);

        await queue.stop();
        await expect(queue.send("test")).rejects.toBe("Connection stopped.");
    });
});
