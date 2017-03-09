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
});
