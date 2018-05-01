import { HttpResponse } from "../src/HttpClient";
import { LogLevel } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
import { NullLogger } from "../src/Loggers";
import { LongPollingTransport } from "../src/LongPollingTransport";
import { ConsoleLogger } from "../src/Utils";

import { TestHttpClient } from "./TestHttpClient";
import { asyncit as it, PromiseSource } from "./Utils";

describe("LongPollingTransport", () => {
    it("shuts down poll after timeout even if server doesn't shut it down on receiving the DELETE", async () => {
        let firstPoll = true;
        const pollCompleted = new PromiseSource();
        const client = new TestHttpClient()
            .on("GET", async (r) => {
                if (firstPoll) {
                    firstPoll = false;
                    return new HttpResponse(200);
                } else {
                    // Turn 'onabort' into a promise.
                    const abort = new Promise((resolve, reject) => r.abortSignal.onabort = resolve);
                    await abort;

                    // Signal that the poll has completed.
                    pollCompleted.resolve();
                    return new HttpResponse(204);
                }
            })
            .on("DELETE", (r) => new HttpResponse(202));
        const transport = new LongPollingTransport(client, null, NullLogger.instance, false, 100);

        await transport.connect("http://example.com", TransferFormat.Text);
        await transport.stop();

        // This should complete within the shutdown timeout
        await pollCompleted.promise;
    });

    it("sends DELETE request on stop", async () => {
        let firstPoll = true;
        const deleteReceived = new PromiseSource();
        const pollCompleted = new PromiseSource();
        const client = new TestHttpClient()
            .on("GET", async (r) => {
                if (firstPoll) {
                    firstPoll = false;
                    return new HttpResponse(200);
                } else {
                    await deleteReceived.promise;
                    pollCompleted.resolve();
                    return new HttpResponse(204);
                }
            })
            .on("DELETE", (r) => {
                deleteReceived.resolve();
                return new HttpResponse(202);
            });
        const transport = new LongPollingTransport(client, null, NullLogger.instance, false);

        await transport.connect("http://example.com", TransferFormat.Text);
        await transport.stop();

        // This should complete, because the DELETE request triggers it to stop.
        await pollCompleted.promise;
    });
});