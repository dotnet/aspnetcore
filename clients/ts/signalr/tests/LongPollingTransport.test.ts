import { HttpError, TimeoutError } from "../src/Errors";
import { HttpResponse } from "../src/HttpClient";
import { LogLevel } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
import { NullLogger } from "../src/Loggers";
import { LongPollingTransport } from "../src/LongPollingTransport";
import { ConsoleLogger } from "../src/Utils";

import { TestHttpClient } from "./TestHttpClient";
import { delay, PromiseSource, SyncPoint } from "./Utils";

describe("LongPollingTransport", () => {
    it("shuts down polling by aborting in-progress request", async () => {
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

                    return new HttpResponse(200);
                }
            })
            .on("DELETE", (r) => new HttpResponse(202));
        const transport = new LongPollingTransport(client, null, NullLogger.instance, false);

        await transport.connect("http://example.com", TransferFormat.Text);
        const stopPromise = transport.stop();

        await pollCompleted.promise;

        await stopPromise;
    });

    it("204 server response stops polling and raises onClose", async () => {
        let firstPoll = true;
        let onCloseCalled = false;
        const client = new TestHttpClient()
            .on("GET", async (r) => {
                if (firstPoll) {
                    firstPoll = false;
                    return new HttpResponse(200);
                } else {
                    // A 204 response will stop the long polling transport
                    return new HttpResponse(204);
                }
            });
        const transport = new LongPollingTransport(client, null, NullLogger.instance, false);

        const stopPromise = makeClosedPromise(transport);

        await transport.connect("http://example.com", TransferFormat.Text);

        // Close will be called on transport because of 204 result from polling
        await stopPromise;
    });

    it("sends DELETE on stop after polling has finished", async () => {
        let firstPoll = true;
        let deleteSent = false;
        const pollingPromiseSource = new PromiseSource();
        const deleteSyncPoint = new SyncPoint();
        const httpClient = new TestHttpClient()
            .on("GET", async (r) => {
                if (firstPoll) {
                    firstPoll = false;
                    return new HttpResponse(200);
                } else {
                    await pollingPromiseSource.promise;
                    return new HttpResponse(204);
                }
            })
            .on("DELETE", async (r) => {
                deleteSent = true;
                await deleteSyncPoint.waitToContinue();
                return new HttpResponse(202);
            });
    
        const transport = new LongPollingTransport(httpClient, null, NullLogger.instance, false);

        await transport.connect("http://tempuri.org", TransferFormat.Text);

        // Begin stopping transport
        const stopPromise = transport.stop();

        // Delete will not be sent until polling is finished
        expect(deleteSent).toEqual(false);

        // Allow polling to complete
        pollingPromiseSource.resolve();

        // Wait for delete to be called
        await deleteSyncPoint.waitForSyncPoint();

        expect(deleteSent).toEqual(true);

        deleteSyncPoint.continue();

        // Wait for stop to complete
        await stopPromise;
    });
});

function makeClosedPromise(transport: LongPollingTransport): Promise<void> {
    const closed = new PromiseSource();
    transport.onclose = (error) => {
        if (error) {
            closed.reject(error);
        } else {
            closed.resolve();
        }
    };
    return closed.promise;
}