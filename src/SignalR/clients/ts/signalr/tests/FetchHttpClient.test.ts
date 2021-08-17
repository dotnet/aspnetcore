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
});
