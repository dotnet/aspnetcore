// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AccessTokenHttpClient } from "../src/AccessTokenHttpClient";
import { HttpError } from "../src/Errors";
import { HttpResponse } from "../src/HttpClient";
import { TestHttpClient } from "./TestHttpClient";

import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

describe("AccessTokenHttpClient", () => {
    it("retries with a new token when the response is a 401", async () => {
        let requestCount = 0;
        let tokenCount = 0;
        const innerClient = new TestHttpClient()
            .on("GET", "http://example.com/prime", () => new HttpResponse(200, "OK", ""))
            .on("GET", "http://example.com", () => {
                requestCount++;
                return requestCount === 1
                    ? new HttpResponse(401, "Unauthorized", "")
                    : new HttpResponse(200, "OK", "");
            });
        const client = new AccessTokenHttpClient(innerClient, () => `token${++tokenCount}`);

        // the first send always fetches a fresh token and is not allowed to retry,
        // so prime the client before exercising the retry behavior
        await client.send({ method: "GET", url: "http://example.com/prime" });
        const response = await client.send({ method: "GET", url: "http://example.com" });

        expect(response.statusCode).toBe(200);
        expect(requestCount).toBe(2);
        expect(tokenCount).toBe(2);
        expect(innerClient.sentRequests[innerClient.sentRequests.length - 1].headers).toEqual({
            Authorization: "Bearer token2",
        });
    });

    it("retries with a new token when the inner client throws an HttpError for a 401", async () => {
        let requestCount = 0;
        let tokenCount = 0;
        const innerClient = new TestHttpClient()
            .on("GET", "http://example.com/prime", () => new HttpResponse(200, "OK", ""))
            .on("GET", "http://example.com", () => {
                requestCount++;
                if (requestCount === 1) {
                    throw new HttpError("Unauthorized", 401);
                }
                return new HttpResponse(200, "OK", "");
            });
        const client = new AccessTokenHttpClient(innerClient, () => `token${++tokenCount}`);

        // the first send always fetches a fresh token and is not allowed to retry,
        // so prime the client before exercising the retry behavior
        await client.send({ method: "GET", url: "http://example.com/prime" });
        const response = await client.send({ method: "GET", url: "http://example.com" });

        expect(response.statusCode).toBe(200);
        expect(requestCount).toBe(2);
        expect(tokenCount).toBe(2);
        expect(innerClient.sentRequests[innerClient.sentRequests.length - 1].headers).toEqual({
            Authorization: "Bearer token2",
        });
    });

    it("does not retry more than once when the retried request also throws a 401", async () => {
        let requestCount = 0;
        let tokenCount = 0;
        const innerClient = new TestHttpClient()
            .on("GET", "http://example.com/prime", () => new HttpResponse(200, "OK", ""))
            .on("GET", "http://example.com", () => {
                requestCount++;
                throw new HttpError("Unauthorized", 401);
            });
        const client = new AccessTokenHttpClient(innerClient, () => `token${++tokenCount}`);

        await client.send({ method: "GET", url: "http://example.com/prime" });
        await expect(client.send({ method: "GET", url: "http://example.com" }))
            .rejects.toEqual(new HttpError("Unauthorized", 401));

        // the initial request and a single retry, but no further attempts
        expect(requestCount).toBe(2);
        expect(tokenCount).toBe(2);
    });

    it("does not retry when the inner client throws a non-401 HttpError", async () => {
        let requestCount = 0;
        let tokenCount = 0;
        const innerClient = new TestHttpClient()
            .on("GET", "http://example.com", () => {
                requestCount++;
                throw new HttpError("Internal Server Error", 500);
            });
        const client = new AccessTokenHttpClient(innerClient, () => `token${++tokenCount}`);

        await expect(client.send({ method: "GET", url: "http://example.com" }))
            .rejects.toEqual(new HttpError("Internal Server Error", 500));
        expect(requestCount).toBe(1);
    });

    it("does not retry when there is no access token factory", async () => {
        let requestCount = 0;
        const innerClient = new TestHttpClient()
            .on("GET", "http://example.com", () => {
                requestCount++;
                throw new HttpError("Unauthorized", 401);
            });
        const client = new AccessTokenHttpClient(innerClient, undefined);

        await expect(client.send({ method: "GET", url: "http://example.com" }))
            .rejects.toEqual(new HttpError("Unauthorized", 401));
        expect(requestCount).toBe(1);
    });

    it("does not retry negotiate requests when the inner client throws a 401", async () => {
        let requestCount = 0;
        let tokenCount = 0;
        const innerClient = new TestHttpClient()
            .on("POST", /negotiate/, () => {
                requestCount++;
                throw new HttpError("Unauthorized", 401);
            });
        const client = new AccessTokenHttpClient(innerClient, () => `token${++tokenCount}`);

        await expect(client.send({ method: "POST", url: "http://example.com/negotiate?negotiateVersion=1" }))
            .rejects.toEqual(new HttpError("Unauthorized", 401));
        expect(requestCount).toBe(1);
        // the token was fetched once for the request itself, but not refreshed for a retry
        expect(tokenCount).toBe(1);
    });
});
