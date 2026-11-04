// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpRequest } from "../src/HttpClient";
import { TestHttpClient } from "./TestHttpClient";
import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

describe("HttpClient", () => {
    describe("get", () => {
        it("sets the method and URL appropriately", async () => {
            let request!: HttpRequest;
            const testClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            await testClient.get("http://localhost");
            expect(request.method).toEqual("GET");
            expect(request.url).toEqual("http://localhost");
        });

        it("overrides method and url in options", async () => {
            let request!: HttpRequest;
            const testClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            await testClient.get("http://localhost", {
                method: "OPTIONS",
                url: "http://wrong",
            });
            expect(request.method).toEqual("GET");
            expect(request.url).toEqual("http://localhost");
        });

        it("copies other options", async () => {
            let request!: HttpRequest;
            const testClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            await testClient.get("http://localhost", {
                headers: { "X-HEADER": "VALUE"},
                timeout: 42,
            });
            expect(request.timeout).toEqual(42);
            expect(request.headers).toEqual({ "X-HEADER": "VALUE"});
        });
    });

    describe("post", () => {
        it("sets the method and URL appropriately", async () => {
            let request!: HttpRequest;
            const testClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            await testClient.post("http://localhost");
            expect(request.method).toEqual("POST");
            expect(request.url).toEqual("http://localhost");
        });

        it("overrides method and url in options", async () => {
            let request!: HttpRequest;
            const testClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            await testClient.post("http://localhost", {
                method: "OPTIONS",
                url: "http://wrong",
            });
            expect(request.method).toEqual("POST");
            expect(request.url).toEqual("http://localhost");
        });

        it("copies other options", async () => {
            let request!: HttpRequest;
            const testClient = new TestHttpClient().on((r) => {
                request = r;
                return "";
            });

            await testClient.post("http://localhost", {
                headers: { "X-HEADER": "VALUE"},
                timeout: 42,
            });
            expect(request.timeout).toEqual(42);
            expect(request.headers).toEqual({ "X-HEADER": "VALUE"});
        });
    });
});
