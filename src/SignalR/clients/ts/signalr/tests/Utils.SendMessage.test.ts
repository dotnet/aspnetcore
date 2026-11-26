// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpError } from "../src/Errors";
import { HttpResponse } from "../src/HttpClient";
import { NullLogger } from "../src/Loggers";
import { sendMessage } from "../src/Utils";
import { TestHttpClient } from "./TestHttpClient";

describe("sendMessage", () => {
    it.each([200, 204, 299])("accepts the successful status code %s", async (statusCode) => {
        const httpClient = new TestHttpClient()
            .on("POST", () => new HttpResponse(statusCode));

        await expect(sendMessage(
            NullLogger.instance,
            "Test",
            httpClient,
            "http://example.com",
            "send data",
            { headers: {}, logMessageContent: false }))
            .resolves
            .toBeUndefined();
    });

    it.each([199, 300, 500])("rejects the non-success status code %s", async (statusCode) => {
        const content = `send failed with ${statusCode}`;
        const httpClient = new TestHttpClient()
            .on("POST", () => new HttpResponse(statusCode, "Send Failed", content));

        await expect(sendMessage(
            NullLogger.instance,
            "Test",
            httpClient,
            "http://example.com",
            "send data",
            { headers: {}, logMessageContent: false }))
            .rejects
            .toEqual(new HttpError(content, statusCode));
    });
});
