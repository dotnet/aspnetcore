import { IHttpClient } from "../Microsoft.AspNetCore.SignalR.Client.TS/HttpClient"
import { Connection } from "../Microsoft.AspNetCore.SignalR.Client.TS/Connection"
import { ISignalROptions } from "../Microsoft.AspNetCore.SignalR.Client.TS/ISignalROptions"

describe("Connection", () => {
    it("starting connection fails if getting id fails", async (done) => {
        let options: ISignalROptions = {
            httpClient: <IHttpClient>{
                get(url: string): Promise<string> {
                    if (url.indexOf("/negotiate") >= 0) {
                        return Promise.reject("error");
                    }
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

        let connection = new Connection("http://tempuri.org", undefined, options);

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
                get(url: string): Promise<string> {
                    if (url.indexOf("/negotiate") >= 0) {
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
                    }
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

        let connection = new Connection("http://tempuri.org", undefined, options);

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
                get(url: string): Promise<string> {
                    if (url.indexOf("/negotiate") >= 0) {
                        return Promise.reject("error");
                    }
                    return Promise.resolve("");
                }
            }
        } as ISignalROptions;

        let connection = new Connection("http://tempuri.org", undefined, options);

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
});
