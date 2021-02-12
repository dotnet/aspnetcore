// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpResponse } from "../src/HttpClient";
import { HttpConnection, INegotiateResponse } from "../src/HttpConnection";
import { IHttpConnectionOptions } from "../src/IHttpConnectionOptions";
import { HttpTransportType, ITransport, TransferFormat } from "../src/ITransport";

import { HttpError } from "../src/Errors";
import { eachEndpointUrl, eachTransport } from "./Common";
import { TestHttpClient } from "./TestHttpClient";
import { PromiseSource } from "./Utils";

const commonOptions: IHttpConnectionOptions = {
    logger: null,
};

const defaultConnectionId = "abc123";
const defaultNegotiateResponse: INegotiateResponse = {
    availableTransports: [
        { transport: "WebSockets", transferFormats: ["Text", "Binary"] },
        { transport: "ServerSentEvents", transferFormats: ["Text"] },
        { transport: "LongPolling", transferFormats: ["Text", "Binary"] },
    ],
    connectionId: defaultConnectionId,
};

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
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => Promise.reject("error"))
                .on("GET", (r) => ""),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        expect(connection.start(TransferFormat.Text))
            .rejects
            .toBe("error");
    });

    it("cannot start a running connection", async () => {
        const negotiating = new PromiseSource();
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => {
                    negotiating.resolve();
                    return defaultNegotiateResponse;
                }),
            transport: {
                connect(url: string, transferFormat: TransferFormat) {
                    return Promise.resolve();
                },
                send(data: any) {
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
                .toThrow("Cannot start a connection that is not in the 'Disconnected' state.");
        } finally {
            await connection.stop();
        }
    });

    it("can start a stopped connection", async () => {
        let negotiateCalls = 0;
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => {
                    negotiateCalls += 1;
                    return Promise.reject("reached negotiate");
                })
                .on("GET", (r) => ""),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toBe("reached negotiate");

        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toBe("reached negotiate");
    });

    it("can stop a starting connection", async () => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", async () => {
                    // tslint:disable-next-line:no-floating-promises
                    connection.stop();
                    return "{}";
                })
                .on("GET", async () => {
                    // tslint:disable-next-line:no-floating-promises
                    connection.stop();
                    return "";
                }),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        await connection.start(TransferFormat.Text);
    });

    it("can stop a non-started connection", async () => {
        const connection = new HttpConnection("http://tempuri.org", commonOptions);
        await connection.stop();
    });

    it("start throws after all transports fail", async () => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [] }))
                .on("GET", (r) => { throw new Error("fail"); }),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org?q=myData", options);

        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("Unable to initialize any of the available transports.");
    });

    it("preserves user's query string", async () => {
        const connectUrl = new PromiseSource<string>();
        const fakeTransport: ITransport = {
            connect(url: string): Promise<void> {
                connectUrl.resolve(url);
                return Promise.resolve();
            },
            send(data: any): Promise<void> {
                return Promise.resolve();
            },
            stop(): Promise<void> {
                return Promise.resolve();
            },
            onclose: undefined,
            onreceive: undefined,
        };

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => "{ \"connectionId\": \"42\" }")
                .on("GET", (r) => ""),
            transport: fakeTransport,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org?q=myData", options);
        try {
            const startPromise = connection.start(TransferFormat.Text);

            expect(await connectUrl).toBe("http://tempuri.org?q=myData&id=42");

            await startPromise;
        } finally {
            await connection.stop();
        }
    });

    eachEndpointUrl((givenUrl: string, expectedUrl: string) => {
        it(`negotiate request for '${givenUrl}' puts 'negotiate' at the end of the path`, async () => {
            const negotiateUrl = new PromiseSource<string>();
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => {
                        negotiateUrl.resolve(r.url);
                        throw new HttpError("We don't care how this turns out", 500);
                    })
                    .on("GET", (r) => {
                        return new HttpResponse(204);
                    })
                    .on("DELETE", (r) => new HttpResponse(202)),
            } as IHttpConnectionOptions;

            const connection = new HttpConnection(givenUrl, options);
            try {
                const startPromise = connection.start(TransferFormat.Text);

                expect(await negotiateUrl).toBe(expectedUrl);

                await expect(startPromise).rejects;
            } finally {
                await connection.stop();
            }
        });
    });

    eachTransport((requestedTransport: HttpTransportType) => {
        it(`cannot be started if requested ${HttpTransportType[requestedTransport]} transport not available on server`, async () => {
            // Clone the default response
            const negotiateResponse = { ...defaultNegotiateResponse };

            // Remove the requested transport from the response
            negotiateResponse.availableTransports = negotiateResponse.availableTransports
                .filter((f) => f.transport !== HttpTransportType[requestedTransport]);

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => negotiateResponse)
                    .on("GET", (r) => new HttpResponse(204)),
                transport: requestedTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Unable to initialize any of the available transports.");
        });

        for (const [val, name] of [[null, "null"], [undefined, "undefined"], [0, "0"]]) {
            it(`can be started using ${HttpTransportType[requestedTransport]} transport when transport mask is ${name}`, async () => {
                const negotiateResponse = {
                    availableTransports: [
                        { transport: "WebSockets", transferFormats: [ "Text", "Binary" ] },
                        { transport: "ServerSentEvents", transferFormats: [ "Text" ] },
                        { transport: "LongPolling", transferFormats: [ "Text", "Binary" ] },
                    ],
                    connectionId: "abc123",
                };

                const options: IHttpConnectionOptions = {
                    ...commonOptions,
                    httpClient: new TestHttpClient()
                        .on("POST", (r) => negotiateResponse)
                        .on("GET", (r) => new HttpResponse(204)),
                    transport: val,
                } as IHttpConnectionOptions;

                const connection = new HttpConnection("http://tempuri.org", options);

                await connection.start(TransferFormat.Text);
            });
        }

        it(`cannot be started if server's only transport (${HttpTransportType[requestedTransport]}) is masked out by the transport option`, async () => {
            const negotiateResponse = {
                availableTransports: [
                    { transport: "WebSockets", transferFormats: [ "Text", "Binary" ] },
                    { transport: "ServerSentEvents", transferFormats: [ "Text" ] },
                    { transport: "LongPolling", transferFormats: [ "Text", "Binary" ] },
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
                    .on("POST", (r) => negotiateResponse)
                    .on("GET", (r) => new HttpResponse(204)),
                transport: transportMask,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);

            try {
                await connection.start(TransferFormat.Text);
                fail("Expected connection.start to throw!");
            } catch (e) {
                expect(e.message).toBe("Unable to initialize any of the available transports.");
            }
        });
    });

    it("cannot be started if no transport available on server and no transport requested", async () => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [] }))
                .on("GET", (r) => ""),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("Unable to initialize any of the available transports.");
    });

    it("does not send negotiate request if WebSockets transport requested explicitly and skipNegotiation is true", async () => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient(),
            skipNegotiation: true,
            transport: HttpTransportType.WebSockets,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("'WebSocket' is not supported in your environment.");
    });

    it("does not start non WebSockets transport requested explicitly and skipNegotiation is true", async () => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient(),
            skipNegotiation: true,
            transport: HttpTransportType.LongPolling,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("Negotiation can only be skipped when using the WebSocket transport directly.");
    });

    it("redirects to url when negotiate returns it", async () => {
        let firstNegotiate = true;
        let firstPoll = true;
        const httpClient = new TestHttpClient()
            .on("POST", /negotiate$/, (r) => {
                if (firstNegotiate) {
                    firstNegotiate = false;
                    return { url: "https://another.domain.url/chat" };
                }
                return {
                    availableTransports: [{ transport: "LongPolling", transferFormats: ["Text"] }],
                    connectionId: "0rge0d00-0040-0030-0r00-000q00r00e00",
                };
            })
            .on("GET", (r) => {
                if (firstPoll) {
                    firstPoll = false;
                    return "";
                }
                return new HttpResponse(204, "No Content", "");
            })
            .on("DELETE", (r) => new HttpResponse(202));

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient,
            transport: HttpTransportType.LongPolling,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start(TransferFormat.Text);

            expect(httpClient.sentRequests.length).toBe(4);
            expect(httpClient.sentRequests[0].url).toBe("http://tempuri.org/negotiate");
            expect(httpClient.sentRequests[1].url).toBe("https://another.domain.url/chat/negotiate");
            expect(httpClient.sentRequests[2].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
            expect(httpClient.sentRequests[3].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
        } finally {
            await connection.stop();
        }
    });

    it("fails to start if negotiate redirects more than 100 times", async () => {
        const httpClient = new TestHttpClient()
            .on("POST", /negotiate$/, (r) => ({ url: "https://another.domain.url/chat" }));

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient,
            transport: HttpTransportType.LongPolling,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("Negotiate redirection limit exceeded.");
    });

    it("redirects to url when negotiate returns it with access token", async () => {
        let firstNegotiate = true;
        let firstPoll = true;
        const httpClient = new TestHttpClient()
            .on("POST", /negotiate$/, (r) => {
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
            .on("DELETE", (r) => new HttpResponse(202));

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            accessTokenFactory: () => "firstSecret",
            httpClient,
            transport: HttpTransportType.LongPolling,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start(TransferFormat.Text);

            expect(httpClient.sentRequests.length).toBe(4);
            expect(httpClient.sentRequests[0].url).toBe("http://tempuri.org/negotiate");
            expect(httpClient.sentRequests[1].url).toBe("https://another.domain.url/chat/negotiate");
            expect(httpClient.sentRequests[2].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
            expect(httpClient.sentRequests[3].url).toMatch(/^https:\/\/another\.domain\.url\/chat\?id=0rge0d00-0040-0030-0r00-000q00r00e00/i);
        } finally {
            await connection.stop();
        }
    });

    it("authorization header removed when token factory returns null and using LongPolling", async () => {
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
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [availableTransport] }))
                .on("GET", (r) => {
                    httpClientGetCount++;
                    // tslint:disable-next-line:no-string-literal
                    const authorizationValue = r.headers["Authorization"];
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
                        throw new Error("fail");
                    }
                })
                .on("DELETE", (r) => new HttpResponse(202)),
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

    it("sets inherentKeepAlive feature when using LongPolling", async () => {
        const availableTransport = { transport: "LongPolling", transferFormats: ["Text"] };

        let httpClientGetCount = 0;
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [availableTransport] }))
                .on("GET", (r) => {
                    httpClientGetCount++;
                    if (httpClientGetCount === 1) {
                        // First long polling request must succeed so start completes
                        return "";
                    } else {
                        throw new Error("fail");
                    }
                })
                .on("DELETE", (r) => new HttpResponse(202)),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start(TransferFormat.Text);
            expect(connection.features.inherentKeepAlive).toBe(true);
        } finally {
            await connection.stop();
        }
    });

    it("does not select ServerSentEvents transport when not available in environment", async () => {
        const serverSentEventsTransport = { transport: "ServerSentEvents", transferFormats: ["Text"] };

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [serverSentEventsTransport] })),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        await expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("Unable to initialize any of the available transports.");
    });

    it("does not select WebSockets transport when not available in environment", async () => {
        const webSocketsTransport = { transport: "WebSockets", transferFormats: ["Text"] };

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [webSocketsTransport] })),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        expect(connection.start(TransferFormat.Text))
            .rejects
            .toThrow("Unable to initialize any of the available transports.");
    });

    describe(".constructor", () => {
        it("throws if no Url is provided", async () => {
            // Force TypeScript to let us call the constructor incorrectly :)
            expect(() => new (HttpConnection as any)()).toThrowError("The 'url' argument is required.");
        });
    });

    describe("startAsync", () => {
        it("throws if an unsupported TransferFormat is provided", async () => {
            // Force TypeScript to let us call start incorrectly
            const connection: any = new HttpConnection("http://tempuri.org", commonOptions);

            expect(() => connection.start(42)).toThrowError("Unknown transferFormat value: 42.");
        });
    });
});
