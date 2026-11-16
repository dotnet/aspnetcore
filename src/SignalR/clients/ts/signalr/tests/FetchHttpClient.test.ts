// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { FetchHttpClient } from "../src/FetchHttpClient";
import { NullLogger } from "../src/Loggers";

describe("FetchHttpClient", () => {
    it("works if global fetch is available but AbortController is not", async () => {
        (global.fetch as any) = () => {
            throw new Error("error from test");
        };
        const httpClient = new FetchHttpClient(NullLogger.instance);

        await expect(httpClient.post("/")).rejects.toEqual(new Error("error from test"));
    });

    it("sets Content-type header for plaintext", async () => {
        (global.fetch as any) = (_: string, request: RequestInit) => {
            expect((request.headers as any)!["Content-Type"]).toEqual("text/plain;charset=UTF-8")
            throw new Error("error from test");
        };
        const httpClient = new FetchHttpClient(NullLogger.instance);

        try {
            await httpClient.post("/", { content: "content" });
        } catch (e) {
            expect(e).toEqual(new Error("error from test"));
        }
    });

    it("sets Content-Type header for binary", async () => {
        (global.fetch as any) = (_: string, request: RequestInit) => {
            expect((request.headers as any)!["Content-Type"]).toEqual("application/octet-stream")
            throw new Error("error from test");
        };
        const httpClient = new FetchHttpClient(NullLogger.instance);

        try {
            await httpClient.post("/", { content: new ArrayBuffer(1) });
        } catch (e) {
            expect(e).toEqual(new Error("error from test"));
        }
    });

    it("does not set Content-Type header for empty content", async () => {
        (global.fetch as any) = (_: string, request: RequestInit) => {
            expect((request.headers as any)!["Content-Type"]).toBeUndefined()
            throw new Error("error from test");
        };
        const httpClient = new FetchHttpClient(NullLogger.instance);

        try {
            await httpClient.post("/", { content: "" });
        } catch (e) {
            expect(e).toEqual(new Error("error from test"));
        }

        try {
            await httpClient.post("/");
        } catch (e) {
            expect(e).toEqual(new Error("error from test"));
        }
    });

    it("returns non-success responses", async () => {
        const content = JSON.stringify({ error: "authentication_refresh_required" });
        (global.fetch as any) = () => Promise.resolve(new Response(content, {
            status: 409,
            statusText: "Conflict",
        }));
        const httpClient = new FetchHttpClient(NullLogger.instance);
        const response = await httpClient.get("/", {
            responseType: "arraybuffer",
        });

        expect(response.statusCode).toEqual(409);
        expect(response.statusText).toEqual("Conflict");
        expect(response.content).toBeInstanceOf(ArrayBuffer);
        expect(String.fromCharCode.apply(
            null,
            Array.prototype.slice.call(new Uint8Array(response.content as ArrayBuffer)))).toEqual(content);
    });

    it("returns text server-error responses", async () => {
        const content = "server error";
        (global.fetch as any) = () => Promise.resolve(new Response(content, {
            status: 500,
            statusText: "Internal Server Error",
        }));
        const httpClient = new FetchHttpClient(NullLogger.instance);

        const response = await httpClient.get("/");

        expect(response.statusCode).toEqual(500);
        expect(response.statusText).toEqual("Internal Server Error");
        expect(response.content).toEqual(content);
    });
});
