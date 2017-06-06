import { IHttpClient } from "../Microsoft.AspNetCore.SignalR.Client.TS/HttpClient"
import { Connection } from "../Microsoft.AspNetCore.SignalR.Client.TS/Connection"
import { ISignalROptions } from "../Microsoft.AspNetCore.SignalR.Client.TS/ISignalROptions"
import { DataReceived, TransportClosed } from "../Microsoft.AspNetCore.SignalR.Client.TS/Common"
import { ITransport } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"

describe("Connection", () => {

    it("starting connection fails if getting id fails", async (done) => {
        let options: ISignalROptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

        let connection = new Connection("http://tempuri.org", options);

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
        let options: ISignalROptions = {
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
        } as ISignalROptions;

        let connection = new Connection("http://tempuri.org", options);

        try {
            await connection.start();
        }
        catch (e) {
            // This exception is thrown after the actual verification is completed.
            // The connection is not setup to be running so just ignore the error.
        }
    });

    it("cannot start a stopped connection", async (done) => {
        let options: ISignalROptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    return Promise.reject("error");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

        let connection = new Connection("http://tempuri.org", options);

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
        let options: ISignalROptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    connection.stop();
                    return Promise.resolve("");
                },
                get(url: string): Promise<string> {
                    connection.stop();
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

        var connection = new Connection("http://tempuri.org", options);

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
        var connection = new Connection("http://tempuri.org");
        await connection.stop();
        done();
    });

    it("preserves users connection string", async done => {
        let options: ISignalROptions = {
            httpClient: <IHttpClient>{
                options(url: string): Promise<string> {
                    return Promise.resolve("42");
                },
                get(url: string): Promise<string> {
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

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

        var connection = new Connection("http://tempuri.org?q=myData", options);

        try {
            await connection.start(fakeTransport);
            fail();
            done();
        }
        catch (e) {
        }

        expect(connectUrl).toBe("http://tempuri.org?q=myData&id=42");
        done();
    });
});
