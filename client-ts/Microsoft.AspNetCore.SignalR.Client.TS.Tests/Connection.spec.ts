// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { IHttpClient } from "../Microsoft.AspNetCore.SignalR.Client.TS/HttpClient"
import { HttpConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/HttpConnection"
import { IHttpConnectionOptions } from "../Microsoft.AspNetCore.SignalR.Client.TS/IHttpConnectionOptions"
import { DataReceived, TransportClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"
import { ITransport, TransportType, TransferMode } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"
import { eachTransport, eachEndpointUrl } from "./Common";

describe("Connection", () => {
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
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            },
            logging: null
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
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    connection.start()
                        .then(() => {
                            fail();
                            done();
                        })
                        .catch((error: Error) => {
                            expect(error.message).toBe("Cannot start a connection that is not in the 'Initial' state.");
                            done();
                        });

                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            },
            logging: null
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

    it("cannot start a stopped connection", async (done) => {
        let options: IHttpConnectionOptions = {
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            },
            logging: null
        } as IHttpConnectionOptions;

        let connection = new HttpConnection("http://tempuri.org", options);

        try {
            // start will fail and transition the connection to the Disconnected state
            await connection.start();
        }
        catch (e) {
            // The connection is not setup to be running so just ignore the error.
        }

        try {
            await connection.start();
            fail();
            done();
        }
        catch (e) {
            expect(e.message).toBe("Cannot start a connection that is not in the 'Initial' state.");
            done();
        }
    });

    it("can stop a starting connection", async (done) => {
        let options: IHttpConnectionOptions = {
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    connection.stop();
                    return Promise.resolve("{}");
                },
                get(url: string): Promise<string> {
                    connection.stop();
                    return Promise.resolve("");
                }
            },
            logging: null
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
            stop(): void { },
            onreceive: undefined,
            onclose: undefined,
        }

        let options: IHttpConnectionOptions = {
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    return Promise.resolve("{ \"connectionId\": \"42\" }");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            },
            transport: fakeTransport,
            logging: null
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
                httpClient: <IHttpClient>{
                    post(url: string): Promise<string> {
                        negotiateUrl = url;
                        connection.stop();
                        return Promise.resolve("{}");
                    },
                    get(url: string): Promise<string> {
                        connection.stop();
                        return Promise.resolve("");
                    }
                },
                logging: null
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
                httpClient: <IHttpClient>{
                    post(url: string): Promise<string> {
                        return Promise.resolve("{ \"connectionId\": \"42\", \"availableTransports\": [] }");
                    },
                    get(url: string): Promise<string> {
                        return Promise.resolve("");
                    }
                },
                transport: requestedTransport,
                logging: null
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
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    return Promise.resolve("{ \"connectionId\": \"42\", \"availableTransports\": [] }");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            },
            logging: null
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
            httpClient: <IHttpClient>{
                post(url: string): Promise<string> {
                    return Promise.reject("Should not be called");
                },
                get(url: string): Promise<string> {
                    return Promise.reject("Should not be called");
                }
            },
            transport: TransportType.WebSockets,
            logging: null
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
                stop(): void { },
                onreceive: null,
                onclose: null,
                mode: transportTransferMode
            } as ITransport;

            let options: IHttpConnectionOptions = {
                httpClient: <IHttpClient>{
                    post(url: string): Promise<string> {
                        return Promise.resolve("{ \"connectionId\": \"42\", \"availableTransports\": [] }");
                    },
                    get(url: string): Promise<string> {
                        return Promise.resolve("");
                    }
                },
                transport: fakeTransport,
                logging: null
            } as IHttpConnectionOptions;

            let connection = new HttpConnection("https://tempuri.org", options);
            connection.features.transferMode = requestedTransferMode;
            await connection.start();
            let actualTransferMode = connection.features.transferMode;

            expect(actualTransferMode).toBe(transportTransferMode);
        });
    });
});
