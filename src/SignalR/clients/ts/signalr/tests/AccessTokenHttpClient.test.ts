// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AccessTokenHttpClient } from "../src/AccessTokenHttpClient";
import { HttpError } from "../src/Errors";
import { HttpRequest, HttpResponse } from "../src/HttpClient";
import { TestHttpClient } from "./TestHttpClient";
import { registerUnhandledRejectionHandler } from "./Utils";
import { VerifyLogger } from "./Common";
import { LongPollingTransport } from "../src/LongPollingTransport";
import { TransferFormat } from "../src/ITransport";

describe("AccessTokenHttpClient", () => {
    beforeAll(() => {
        registerUnhandledRejectionHandler();
    });

    afterAll(() => {
        // Optional cleanup could go here.
    });

    it("retries exactly once on 401 HttpError when accessTokenFactory provided", async () => {
        let call = 0;
        let primed = false;
        const inner = new TestHttpClient();
        inner.on(() => {
            if (!primed) {
                primed = true; // prime request returns 200 and sets initial token
                return new HttpResponse(200, "OK", "prime");
            }
            call++;
            if (call === 1) {
                throw new HttpError("Unauthorized", 401);
            }
            return new HttpResponse(200, "OK", "done");
        });

        let factoryCalls = 0;
        const client = new AccessTokenHttpClient(inner, () => {
            factoryCalls++;
            return `token${factoryCalls}`;
        });

        // Prime token via public API
        await client.get("http://example.com/prime");

        const response = await client.get("http://example.com/resource");
        expect(response.statusCode).toBe(200);
        expect(factoryCalls).toBe(2); // prime + retry refresh
        expect(call).toBe(2); // failing attempt + successful retry
    });

    [403, 500].forEach(status => {
        it(`does not retry on status ${status} HttpError`, async () => {
            let primed = false;
            let failingCalls = 0;
            const inner = new TestHttpClient();
            inner.on(() => {
                if (!primed) {
                    primed = true;
                    return new HttpResponse(200, "OK", "prime");
                }
                failingCalls++;
                throw new HttpError("Error", status);
            });

            let factoryCalls = 0;
            const client = new AccessTokenHttpClient(inner, () => {
                factoryCalls++;
                return `token${factoryCalls}`;
            });

            await client.get("http://example.com/prime");
            try {
                await client.get("http://example.com/resource");
                expect.fail("expected to throw");
            } catch (e: any) {
                expect(e).toBeInstanceOf(HttpError);
                expect(e.statusCode ?? e.status).toBe(status);
            }
            expect(factoryCalls).toBe(1);
            expect(failingCalls).toBe(1);
        });
    });

    it("LongPollingTransport continues running after 401 during poll and refreshes token", async () => {
        await VerifyLogger.run(async (logger) => {
            let pollIteration = 0;
            let primed = false;
            const tokens: string[] = [];
            const accessTokenFactory = () => {
                const t = `tok${tokens.length + 1}`;
                tokens.push(t);
                return t;
            };
            const httpClient = new AccessTokenHttpClient(new TestHttpClient()
                .on("GET", (r: HttpRequest) => {
                    // Prime request separate from polling loop
                    if (!primed && r.url!.includes("/prime")) {
                        primed = true;
                        return new HttpResponse(200, "OK", "prime");
                    }
                    pollIteration++;
                    if (pollIteration === 1) { // initial connect poll
                        return new HttpResponse(200, "OK", "");
                    }
                    if (pollIteration === 2) { // trigger 401 -> retry
                        return new HttpResponse(401);
                    }
                    if (pollIteration === 3) { // post-refresh poll
                        expect(r.headers).toBeDefined();
                        expect(r.headers?.Authorization).toBeDefined();
                        expect(r.headers?.Authorization).toContain(tokens[tokens.length - 1]);
                        return new HttpResponse(204);
                    }
                    return new HttpResponse(204);
                }), accessTokenFactory);

            // Prime token using public API
            await httpClient.get("http://example.com/prime");

            const transport = new LongPollingTransport(httpClient, logger, { withCredentials: true, headers: {}, logMessageContent: false });
            await transport.connect("http://example.com?connectionId=abc", TransferFormat.Text);
            await transport.stop();

            expect(tokens.length).toBe(2); // primed + refreshed
            expect(pollIteration).toBeGreaterThanOrEqual(3);
        });
    });

    it("retries once on 401 HttpResponse status (non-throwing path)", async () => {
        let primed = false;
        let attempts = 0;
        let retryAuthHeader: string | undefined;
        const inner = new TestHttpClient();
        inner.on((r: HttpRequest) => {
            if (!primed && r.url!.includes("/prime")) {
                primed = true;
                return new HttpResponse(200, "OK", "prime");
            }
            attempts++;
            if (attempts === 1) {
                return new HttpResponse(401);
            }
            // second attempt after refresh
            retryAuthHeader = r.headers?.Authorization;
            return new HttpResponse(200, "OK", "after-retry");
        });

        let factoryCalls = 0;
        const client = new AccessTokenHttpClient(inner, () => {
            factoryCalls++;
            return `token${factoryCalls}`;
        });

        await client.get("http://example.com/prime");
        const resp = await client.get("http://example.com/resource");
        expect(resp.statusCode).toBe(200);
        expect(factoryCalls).toBe(2); // prime + refresh
        expect(attempts).toBe(2); // original 401 + retry 200
        expect(retryAuthHeader).toContain("token2");
    });

    it("does not retry when allowRetry is false (initial token acquisition)", async () => {
        let sends = 0;
        const inner = new TestHttpClient();
        inner.on(() => {
            sends++;
            return new HttpResponse(401);
        });

        let factoryCalls = 0;
        const client = new AccessTokenHttpClient(inner, () => {
            factoryCalls++;
            return `token${factoryCalls}`; // Token factory returns a token string for each call.
        });

        const request: HttpRequest = { method: "GET", url: "http://example.com/resource" };
        const resp = await client.send(request); // send path with existing logic; allowRetry=false triggered by initial token acquisition above
        expect(resp.statusCode).toBe(401);
        expect(factoryCalls).toBe(1);
        expect(sends).toBe(1);
    });

    it("does not retry when refreshed token is empty", async () => {
        let primed = false;
        let attempts = 0;
        const inner = new TestHttpClient();
        inner.on((r: HttpRequest) => {
            if (!primed && r.url!.includes("/prime")) {
                primed = true;
                return new HttpResponse(200, "OK", "prime");
            }
            attempts++;
            return new HttpResponse(401); // cause retry path
        });

        let factoryCalls = 0;
        const client = new AccessTokenHttpClient(inner, () => {
            factoryCalls++;
            if (factoryCalls === 1) {
                return "tok1"; // prime
            }
            return ""; // refresh returns empty -> should not retry send again
        });

        await client.get("http://example.com/prime");
        const resp = await client.get("http://example.com/resource");
        expect(resp.statusCode).toBe(401); // original response returned
        expect(factoryCalls).toBe(2); // prime + attempted refresh
        expect(attempts).toBe(1); // no second send
    });

    it("retries once when HttpError.status is string '401'", async () => {
        let primed = false;
        let attempt = 0;
        let retryAuth: string | undefined;
        const inner = new TestHttpClient();
        inner.on((r: HttpRequest) => {
            if (!primed && r.url!.includes("/prime")) {
                primed = true;
                return new HttpResponse(200, "OK", "prime");
            }
            attempt++;
            if (attempt === 1) {
                const err: any = new Error("Unauthorized: Status code '401'");
                err.name = "HttpError"; // mimic HttpError shape without statusCode
                err.status = "401"; // string status to trigger normalization path
                throw err;
            }
            retryAuth = r.headers?.Authorization;
            return new HttpResponse(200, "OK", "ok");
        });

        let factoryCalls = 0;
        const client = new AccessTokenHttpClient(inner, () => {
            factoryCalls++;
            return `token${factoryCalls}`;
        });

        await client.get("http://example.com/prime");
        const resp = await client.get("http://example.com/resource");
        expect(resp.statusCode).toBe(200);
        expect(factoryCalls).toBe(2); // prime + refresh after string status retry
        expect(attempt).toBe(2); // original throw + retry
        expect(retryAuth).toContain("token2");
    });
});
