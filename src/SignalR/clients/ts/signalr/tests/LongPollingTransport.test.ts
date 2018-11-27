import { HttpError, TimeoutError } from "../src/Errors";
import { HttpResponse } from "../src/HttpClient";
import { LogLevel } from "../src/ILogger";
import { TransferFormat } from "../src/ITransport";
import { NullLogger } from "../src/Loggers";
import { LongPollingTransport } from "../src/LongPollingTransport";
import { ConsoleLogger } from "../src/Utils";

import { TestHttpClient } from "./TestHttpClient";
import { delay, PromiseSource } from "./Utils";

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
                    // Force the shutdown timer to be registered by not returning inline
                    await delay(10);
                    pollCompleted.resolve();
                    return new HttpResponse(204);
                }
            })
            .on("DELETE", (r) => {
                deleteReceived.resolve();
                return new HttpResponse(202);
            });
        const transport = new LongPollingTransport(client, null, NullLogger.instance, false, 100);

        await transport.connect("http://example.com", TransferFormat.Text);
        await transport.stop();

        // This should complete, because the DELETE request triggers it to stop.
        await pollCompleted.promise;
    });

    for (const result of [200, 204, 300, new HttpError("Boom", 500), new TimeoutError()]) {

        // Function has a name property but TypeScript doesn't know about it.
        const resultName = typeof result === "number" ? result.toString() : (result.constructor as any).name;

        it(`does not fire shutdown timer when poll terminates with ${resultName}`, async () => {
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
                        // Force the shutdown timer to be registered by not returning inline
                        await delay(10);
                        pollCompleted.resolve();

                        if (typeof result === "number") {
                            return new HttpResponse(result);
                        } else {
                            throw result;
                        }
                    }
                })
                .on("DELETE", (r) => {
                    deleteReceived.resolve();
                    return new HttpResponse(202);
                });
            const logMessages: string[] = [];
            const transport = new LongPollingTransport(client, null, {
                log(level: LogLevel, message: string) {
                    logMessages.push(message);
                },
            }, false, 100);

            await transport.connect("http://example.com", TransferFormat.Text);
            await transport.stop();

            // This should complete, because the DELETE request triggers it to stop.
            await pollCompleted.promise;

            // Wait for the shutdown timeout to elapse
            // This can be much cleaner when we port to Jest because it has a built-in set of
            // fake timers!
            await delay(150);

            // The pollAbort token should be left unaborted because we shut down gracefully.
            expect(transport.pollAborted).toBe(false);
        });
    }
});
