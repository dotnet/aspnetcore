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

        try {
            await httpClient.post("/");
        } catch (e) {
            expect(e).toEqual(new Error("error from test"));
        }
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
});
