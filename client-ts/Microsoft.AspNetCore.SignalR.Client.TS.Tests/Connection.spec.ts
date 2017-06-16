import { IHttpClient } from "../Microsoft.AspNetCore.SignalR.Client.TS/HttpClient"
import { HttpConnection } from "../Microsoft.AspNetCore.SignalR.Client.TS/HttpConnection"
import { IHttpConnectionOptions } from "../Microsoft.AspNetCore.SignalR.Client.TS/IHttpConnectionOptions"
import { DataReceived, TransportClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"
import { ITransport, TransportType } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"
import { eachTransport } from "./Common";

describe("Connection", () => {

    it("starting connection fails if getting id fails", async (done) => {
        let options: IHttpConnectionOptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            }
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
                options(url: string): Promise<string> {
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
            }
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
                options(url: string): Promise<string> {
                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            }
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
                options(url: string): Promise<string> {
                    connection.stop();
                    return Promise.resolve("{}");
                },
                get(url: string): Promise<string> {
                    connection.stop();
                    return Promise.resolve("");
                }
            }
        } as IHttpConnectionOptions;

        var connection = new HttpConnection("http://tempuri.org", options);

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
        var connection = new HttpConnection("http://tempuri.org");
        await connection.stop();
        done();
    });

    it("preserves users connection string", async done => {
        let connectUrl: string;
        let fakeTransport: ITransport = {
            connect(url: string): Promise<void> {
                connectUrl = url;
                return Promise.reject("");
            },
            send(data: any): Promise<void> {
                return Promise.reject("");
            },
            stop(): void { },
            onDataReceived: undefined,
            onClosed: undefined
        }

        let options: IHttpConnectionOptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    return Promise.resolve("{ \"connectionId\": \"42\" }");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            },
            transport: fakeTransport
        } as IHttpConnectionOptions;


        var connection = new HttpConnection("http://tempuri.org?q=myData", options);

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

    eachTransport((requestedTransport: TransportType) => {
        it(`Connection cannot be started if requested ${TransportType[requestedTransport]} transport not available on server`, async done => {
            let options: IHttpConnectionOptions = {
                httpClient: <IHttpClient>{
                    options(url: string): Promise<string> {
                        return Promise.resolve("{ \"connectionId\": \"42\", \"availableTransports\": [] }");
                    },
                    get(url: string): Promise<string> {
                        return Promise.resolve("");
                    }
                },
                transport: requestedTransport
            } as IHttpConnectionOptions;

            var connection = new HttpConnection("http://tempuri.org", options);
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

    it(`Connection cannot be started if no transport available on server and no transport requested`, async done => {
        let options: IHttpConnectionOptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    return Promise.resolve("{ \"connectionId\": \"42\", \"availableTransports\": [] }");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            }
        } as IHttpConnectionOptions;

        var connection = new HttpConnection("http://tempuri.org", options);
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
