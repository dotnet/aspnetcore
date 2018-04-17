// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DataReceived, TransportClosed } from "../src/Common";
import { HttpConnection } from "../src/HttpConnection";
import { IHttpConnectionOptions } from "../src/HttpConnection";
import { HttpResponse } from "../src/index";
import { ITransport, TransferFormat, HttpTransportType } from "../src/ITransport";
import { eachEndpointUrl, eachTransport } from "./Common";
import { TestHttpClient } from "./TestHttpClient";

const commonOptions: IHttpConnectionOptions = {
    logger: null,
};

describe("HttpConnection", () => {
    it("cannot be created with relative url if document object is not present", () => {
        expect(() => new HttpConnection("/test", commonOptions))
            .toThrow(new Error("Cannot resolve '/test'."));
    });

    it("cannot be created with relative url if window object is not present", () => {
        (global as any).window = {};
        expect(() => new HttpConnection("/test", commonOptions))
            .toThrow(new Error("Cannot resolve '/test'."));
        delete (global as any).window;
    });

    it("starting connection fails if getting id fails", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => Promise.reject("error"))
                .on("GET", (r) => ""),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
            expect(e).toBe("error");
            done();
        }
    });

    it("cannot start a running connection", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => {
                    connection.start(TransferFormat.Text)
                        .then(() => {
                            fail();
                            done();
                        })
                        .catch((error: Error) => {
                            expect(error.message).toBe("Cannot start a connection that is not in the 'Disconnected' state.");
                            done();
                        });
                    return Promise.reject("error");
                }),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
        } catch (e) {
            // This exception is thrown after the actual verification is completed.
            // The connection is not setup to be running so just ignore the error.
        }
    });

    it("can start a stopped connection", async (done) => {
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

        try {
            await connection.start(TransferFormat.Text);
        } catch (e) {
            expect(e).toBe("reached negotiate");
        }

        try {
            await connection.start(TransferFormat.Text);
        } catch (e) {
            expect(e).toBe("reached negotiate");
        }

        done();
    });

    it("can stop a starting connection", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => {
                    connection.stop();
                    return "{}";
                })
                .on("GET", (r) => {
                    connection.stop();
                    return "";
                }),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
            done();
        } catch (e) {
            fail();
            done();
        }
    });

    it("can stop a non-started connection", async (done) => {
        const connection = new HttpConnection("http://tempuri.org", commonOptions);
        await connection.stop();
        done();
    });

    it("start throws after all transports fail", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [] }))
                .on("GET", (r) => { throw new Error("fail"); }),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org?q=myData", options);
        try {
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
            expect(e.message).toBe("Unable to initialize any of the available transports.");
        }
        done();
    });

    it("preserves user's query string", async (done) => {
        let connectUrl: string;
        const fakeTransport: ITransport = {
            connect(url: string): Promise<void> {
                connectUrl = url;
                return Promise.reject("");
            },
            send(data: any): Promise<void> {
                return Promise.reject("");
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
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
        }

        expect(connectUrl).toBe("http://tempuri.org?q=myData&id=42");
        done();
    });

    eachEndpointUrl((givenUrl: string, expectedUrl: string) => {
        it("negotiate request puts 'negotiate' at the end of the path", async (done) => {
            let negotiateUrl: string;
            let connection: HttpConnection;
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => {
                        negotiateUrl = r.url;
                        connection.stop();
                        return "{}";
                    })
                    .on("GET", (r) => {
                        connection.stop();
                        return "";
                    }),
            } as IHttpConnectionOptions;

            connection = new HttpConnection(givenUrl, options);

            try {
                await connection.start(TransferFormat.Text);
                done();
            } catch (e) {
                fail();
                done();
            }

            expect(negotiateUrl).toBe(expectedUrl);
        });
    });

    eachTransport((requestedTransport: HttpTransportType) => {
        // OPTIONS is not sent when WebSockets transport is explicitly requested
        if (requestedTransport === HttpTransportType.WebSockets) {
            return;
        }
        it(`cannot be started if requested ${HttpTransportType[requestedTransport]} transport not available on server`, async (done) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => ({ connectionId: "42", availableTransports: [] }))
                    .on("GET", (r) => ""),
                transport: requestedTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start(TransferFormat.Text);
                fail();
                done();
            } catch (e) {
                expect(e.message).toBe("Unable to initialize any of the available transports.");
                done();
            }
        });
    });

    it("cannot be started if no transport available on server and no transport requested", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [] }))
                .on("GET", (r) => ""),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
            expect(e.message).toBe("Unable to initialize any of the available transports.");
            done();
        }
    });

    it("does not send negotiate request if WebSockets transport requested explicitly", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient(),
            transport: HttpTransportType.WebSockets,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
            // WebSocket is created when the transport is connecting which happens after
            // negotiate request would be sent. No better/easier way to test this.
            expect(e.message).toBe("'WebSocket' is not supported in your environment.");
            done();
        }
    });

    it("authorization header removed when token factory returns null and using LongPolling", async (done) => {
        const availableTransport = { transport: "LongPolling", transferFormats: ["Text"] };

        var httpClientGetCount = 0;
        var accessTokenFactoryCount = 0;
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [availableTransport] }))
                .on("GET", (r) => {
                    httpClientGetCount++;
                    const authorizationValue = r.headers["Authorization"];
                    if (httpClientGetCount == 1) {
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
                }),
            accessTokenFactory: () => {
                accessTokenFactoryCount++;
                if (accessTokenFactoryCount == 1) {
                    return "A token value";
                } else {
                    // Return a null value after the first call to test the header being removed
                    return null;
                }
            },
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
            expect(httpClientGetCount).toBeGreaterThanOrEqual(2);
            expect(accessTokenFactoryCount).toBeGreaterThanOrEqual(2);
            done();
        } catch (e) {
            fail(e);
            done();
        }
    });

    it("sets inherentKeepAlive feature when using LongPolling", async (done) => {
        const availableTransport = { transport: "LongPolling", transferFormats: ["Text"] };

        var httpClientGetCount = 0;
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [availableTransport] }))
                .on("GET", (r) => {
                    httpClientGetCount++;
                    if (httpClientGetCount == 1) {
                        // First long polling request must succeed so start completes
                        return "";
                    } else {
                        throw new Error("fail");
                    }
                }),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
            expect(connection.features.inherentKeepAlive).toBe(true);
            done();
        } catch (e) {
            fail(e);
            done();
        }
    });

    it("does not select ServerSentEvents transport when not available in environment", async (done) => {
        const serverSentEventsTransport = { transport: "ServerSentEvents", transferFormats: ["Text"] };

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [serverSentEventsTransport] })),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
            // ServerSentEvents is only transport returned from server but is not selected
            // because there is no support in the environment, leading to the following error
            expect(e.message).toBe("Unable to initialize any of the available transports.");
            done();
        }
    });

    it("does not select WebSockets transport when not available in environment", async (done) => {
        const webSocketsTransport = { transport: "WebSockets", transferFormats: ["Text"] };

        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => ({ connectionId: "42", availableTransports: [webSocketsTransport] })),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start(TransferFormat.Text);
            fail();
            done();
        } catch (e) {
            // WebSockets is only transport returned from server but is not selected
            // because there is no support in the environment, leading to the following error
            expect(e.message).toBe("Unable to initialize any of the available transports.");
            done();
        }
    });

    describe(".constructor", () => {
        it("throws if no Url is provided", async () => {
            // Force TypeScript to let us call the constructor incorrectly :)
            expect(() => new (HttpConnection as any)()).toThrowError("The 'url' argument is required.");
        });
    });

    describe("startAsync", () => {
        it("throws if no TransferFormat is provided", async () => {
            // Force TypeScript to let us call start incorrectly
            const connection: any = new HttpConnection("http://tempuri.org", commonOptions);

            expect(() => connection.start()).toThrowError("The 'transferFormat' argument is required.");
        });

        it("throws if an unsupported TransferFormat is provided", async () => {
            // Force TypeScript to let us call start incorrectly
            const connection: any = new HttpConnection("http://tempuri.org", commonOptions);

            expect(() => connection.start(42)).toThrowError("Unknown transferFormat value: 42.");
        });
    });
});
