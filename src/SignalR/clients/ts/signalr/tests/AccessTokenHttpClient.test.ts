// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AccessTokenHttpClient } from "../src/AccessTokenHttpClient";
import { HttpError } from "../src/Errors";
import { HttpClient, HttpRequest, HttpResponse } from "../src/HttpClient";
import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

class ThrowingHttpClient extends HttpClient {
    private _handler: (request: HttpRequest) => Promise<HttpResponse>;

    constructor(handler: (request: HttpRequest) => Promise<HttpResponse>) {
        super();
        this._handler = handler;
    }

    public send(request: HttpRequest): Promise<HttpResponse> {
        return this._handler(request);
    }
}

describe("AccessTokenHttpClient", () => {
    it("retries with new token when inner client throws HttpError with 401", async () => {
        let callCount = 0;
        let tokenCallCount = 0;
        const innerClient = new ThrowingHttpClient(async () => {
            callCount++;
            if (callCount === 1) {
                throw new HttpError("Unauthorized", 401);
            }
            return new HttpResponse(200, "OK", "");
        });

        const client = new AccessTokenHttpClient(innerClient, async () => {
            tokenCallCount++;
            return `token-${tokenCallCount}`;
        });

        // First call gets a token, set it up
        client._accessToken = "expired-token";

        const response = await client.send({ url: "http://example.com/ping", method: "GET" });
        expect(response.statusCode).toEqual(200);
        expect(callCount).toEqual(2); // first call threw, second succeeded
        expect(tokenCallCount).toEqual(1); // token was refreshed once on 401
    });

    it("throws HttpError for non-401 status codes from inner client", async () => {
        const innerClient = new ThrowingHttpClient(async () => {
            throw new HttpError("Internal Server Error", 500);
        });

        const client = new AccessTokenHttpClient(innerClient, async () => "token");
        client._accessToken = "valid-token";

        await expect(client.send({ url: "http://example.com/ping", method: "GET" }))
            .rejects.toThrow(HttpError);
    });

    it("does not retry on 401 HttpError when token was just fetched (negotiate)", async () => {
        let callCount = 0;
        const innerClient = new ThrowingHttpClient(async () => {
            callCount++;
            throw new HttpError("Unauthorized", 401);
        });

        const client = new AccessTokenHttpClient(innerClient, async () => "token");

        // Use negotiate URL — should NOT retry
        await expect(client.send({ url: "http://example.com/negotiate?negotiateVersion=1", method: "POST" }))
            .rejects.toThrow(HttpError);
        expect(callCount).toEqual(1); // no retry
    });

    it("retries with new token when inner client returns 401 response (non-throwing)", async () => {
        let callCount = 0;
        let tokenCallCount = 0;
        const innerClient = new ThrowingHttpClient(async () => {
            callCount++;
            if (callCount === 1) {
                return new HttpResponse(401, "Unauthorized", "");
            }
            return new HttpResponse(200, "OK", "");
        });

        const client = new AccessTokenHttpClient(innerClient, async () => {
            tokenCallCount++;
            return `token-${tokenCallCount}`;
        });

        client._accessToken = "expired-token";

        const response = await client.send({ url: "http://example.com/ping", method: "GET" });
        expect(response.statusCode).toEqual(200);
        expect(callCount).toEqual(2);
        expect(tokenCallCount).toEqual(1);
    });
});
