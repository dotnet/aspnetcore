// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { DataReceived, TransportClosed } from "../src/Common";
import { HttpConnection } from "../src/HttpConnection";
import { IHttpConnectionOptions } from "../src/HttpConnection";
import { HttpResponse } from "../src/index";
import { ITransport, TransferMode, TransportType } from "../src/Transports";
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
            await connection.start();
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
                    connection.start()
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
            await connection.start();
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
            await connection.start();
        } catch (e) {
            expect(e).toBe("reached negotiate");
        }

        try {
            await connection.start();
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
            await connection.start();
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

    it("preserves users connection string", async (done) => {
        let connectUrl: string;
        const fakeTransport: ITransport = {
            connect(url: string): Promise<TransferMode> {
                connectUrl = url;
                return Promise.reject(TransferMode.Text);
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
            await connection.start();
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
                await connection.start();
                done();
            } catch (e) {
                fail();
                done();
            }

            expect(negotiateUrl).toBe(expectedUrl);
        });
    });

    eachTransport((requestedTransport: TransportType) => {
        // OPTIONS is not sent when WebSockets transport is explicitly requested
        if (requestedTransport === TransportType.WebSockets) {
            return;
        }
        it(`cannot be started if requested ${TransportType[requestedTransport]} transport not available on server`, async (done) => {
            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => "{ \"connectionId\": \"42\", \"availableTransports\": [] }")
                    .on("GET", (r) => ""),
                transport: requestedTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start();
                fail();
                done();
            } catch (e) {
                expect(e.message).toBe("No available transports found.");
                done();
            }
        });
    });

    it("cannot be started if no transport available on server and no transport requested", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient()
                .on("POST", (r) => "{ \"connectionId\": \"42\", \"availableTransports\": [] }")
                .on("GET", (r) => ""),
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start();
            fail();
            done();
        } catch (e) {
            expect(e.message).toBe("No available transports found.");
            done();
        }
    });

    it("does not send negotiate request if WebSockets transport requested explicitly", async (done) => {
        const options: IHttpConnectionOptions = {
            ...commonOptions,
            httpClient: new TestHttpClient(),
            transport: TransportType.WebSockets,
        } as IHttpConnectionOptions;

        const connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start();
            fail();
            done();
        } catch (e) {
            // WebSocket is created when the transport is connecting which happens after
            // negotiate request would be sent. No better/easier way to test this.
            expect(e.message).toBe("WebSocket is not defined");
            done();
        }
    });

    [
        [TransferMode.Text, TransferMode.Text],
        [TransferMode.Text, TransferMode.Binary],
        [TransferMode.Binary, TransferMode.Text],
        [TransferMode.Binary, TransferMode.Binary],
    ].forEach(([requestedTransferMode, transportTransferMode]) => {
        it(`connection returns ${transportTransferMode} transfer mode when ${requestedTransferMode} transfer mode is requested`, async () => {
            const fakeTransport = {
                // mode: TransferMode : TransferMode.Text
                connect(url: string, requestedTransferMode: TransferMode): Promise<TransferMode> { return Promise.resolve(transportTransferMode); },
                mode: transportTransferMode,
                onclose: null,
                onreceive: null,
                send(data: any): Promise<void> { return Promise.resolve(); },
                stop(): Promise<void> { return Promise.resolve(); },
            } as ITransport;

            const options: IHttpConnectionOptions = {
                ...commonOptions,
                httpClient: new TestHttpClient()
                    .on("POST", (r) => "{ \"connectionId\": \"42\", \"availableTransports\": [] }")
                    .on("GET", (r) => ""),
                transport: fakeTransport,
            } as IHttpConnectionOptions;

            const connection = new HttpConnection("https://tempuri.org", options);
            connection.features.transferMode = requestedTransferMode;
            await connection.start();
            const actualTransferMode = connection.features.transferMode;

            expect(actualTransferMode).toBe(transportTransferMode);
        });
    });
});
