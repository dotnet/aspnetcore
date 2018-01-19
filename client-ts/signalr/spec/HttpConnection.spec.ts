// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { TestHttpClient } from "./TestHttpClient"
import { HttpConnection } from "../src/HttpConnection"
import { IHttpConnectionOptions } from "../src/HttpConnection"
import { DataReceived, TransportClosed } from "../src/Common"
import { ITransport, TransportType, TransferMode } from "../src/Transports"
import { eachTransport, eachEndpointUrl } from "./Common";
import { HttpResponse } from "../src/index";

describe("HttpConnection", () => {
    it("cannot be created with relative url if document object is not present", () => {
        expect(() => new HttpConnection("/test"))
            .toThrow(new Error("Cannot resolve '/test'."));
    });

    it("cannot be created with relative url if window object is not present", () => {
        (<any>global).window = {};
        expect(() => new HttpConnection("/test"))
            .toThrow(new Error("Cannot resolve '/test'."));
        delete (<any>global).window;
    });

    it("starting connection fails if getting id fails", async (done) => {
        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient()
                .on("POST", r => Promise.reject("error"))
                .on("GET", r => ""),
            logger: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start();
            fail();
            done();
        }
        catch (e) {
            expect(e).toBe("error");
            done();
        }
    });

    it("cannot start a running connection", async (done) => {
        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient()
                .on("POST", r => {
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
            logger: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start();
        }
        catch (e) {
            // This exception is thrown after the actual verification is completed.
            // The connection is not setup to be running so just ignore the error.
        }
    });

    it("can start a stopped connection", async (done) => {
        let negotiateCalls = 0;
        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient()
                .on("POST", r => {
                    negotiateCalls += 1;
                    return Promise.reject("reached negotiate");
                })
                .on("GET", r => ""),
            logger: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);

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
        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient()
                .on("POST", r => {
                    connection.stop();
                    return "{}";
                })
                .on("GET", r => {
                    connection.stop();
                    return "";
                }),
            logger: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);

        try {
            await connection.start();
            done();
        }
        catch (e) {
            fail();
            done();
        }
    });

    it("can stop a non-started connection", async (done) => {
        let connection = new HttpConnection("http://tempuri.org");
        await connection.stop();
        done();
    });

    it("preserves users connection string", async done => {
        let connectUrl: string;
        let fakeTransport: ITransport = {
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
            onreceive: undefined,
            onclose: undefined,
        }

        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient()
                .on("POST", r => "{ \"connectionId\": \"42\" }")
                .on("GET", r => ""),
            transport: fakeTransport,
            logger: null,
        } as IHttpConnectionOptions;


        let connection = new HttpConnection("http://tempuri.org?q=myData", options);

        try {
            await connection.start();
            fail();
            done();
        }
        catch (e) {
        }

        expect(connectUrl).toBe("http://tempuri.org?q=myData&id=42");
        done();
    });

    eachEndpointUrl((givenUrl: string, expectedUrl: string) => {
        it("negotiate request puts 'negotiate' at the end of the path", async done => {
            let negotiateUrl: string;
            let connection: HttpConnection;
            let options: IHttpConnectionOptions = {
                httpClient: new TestHttpClient()
                    .on("POST", r => {
                        negotiateUrl = r.url;
                        connection.stop();
                        return "{}";
                    })
                    .on("GET", r => {
                        connection.stop();
                        return "";
                    }),
                logger: null
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
        it(`cannot be started if requested ${TransportType[requestedTransport]} transport not available on server`, async done => {
            let options: IHttpConnectionOptions = {
                httpClient: new TestHttpClient()
                    .on("POST", r => "{ \"connectionId\": \"42\", \"availableTransports\": [] }")
                    .on("GET", r => ""),
                transport: requestedTransport,
                logger: null
            } as IHttpConnectionOptions;

            let connection = new HttpConnection("http://tempuri.org", options);
            try {
                await connection.start();
                fail();
                done();
            }
            catch (e) {
                expect(e.message).toBe("No available transports found.");
                done();
            }
        });
    });

    it("cannot be started if no transport available on server and no transport requested", async done => {
        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient()
                .on("POST", r => "{ \"connectionId\": \"42\", \"availableTransports\": [] }")
                .on("GET", r => ""),
            logger: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start();
            fail();
            done();
        }
        catch (e) {
            expect(e.message).toBe("No available transports found.");
            done();
        }
    });

    it('does not send negotiate request if WebSockets transport requested explicitly', async done => {
        let options: IHttpConnectionOptions = {
            httpClient: new TestHttpClient(),
            transport: TransportType.WebSockets,
            logger: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);
        try {
            await connection.start();
            fail();
            done();
        }
        catch (e) {
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
            let fakeTransport = {
                // mode: TransferMode : TransferMode.Text
                connect(url: string, requestedTransferMode: TransferMode): Promise<TransferMode> { return Promise.resolve(transportTransferMode); },
                send(data: any): Promise<void> { return Promise.resolve(); },
                stop(): Promise<void> { return Promise.resolve(); },
                onreceive: null,
                onclose: null,
                mode: transportTransferMode
            } as ITransport;

            let options: IHttpConnectionOptions = {
                httpClient: new TestHttpClient()
                    .on("POST", r => "{ \"connectionId\": \"42\", \"availableTransports\": [] }")
                    .on("GET", r => ""),
                transport: fakeTransport,
                logger: null
            } as IHttpConnectionOptions;

            let connection = new HttpConnection("https://tempuri.org", options);
            connection.features.transferMode = requestedTransferMode;
            await connection.start();
            let actualTransferMode = connection.features.transferMode;

            expect(actualTransferMode).toBe(transportTransferMode);
        });
    });
});
