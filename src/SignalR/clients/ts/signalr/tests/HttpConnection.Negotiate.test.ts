// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpConnection } from "../src/HttpConnection";
import { HttpResponse } from "../src/HttpClient";
import { IHttpConnectionOptions } from "../src/IHttpConnectionOptions";
import { TransferFormat } from "../src/ITransport";
import { VerifyLogger } from "./Common";
import { TestHttpClient } from "./TestHttpClient";
import { registerUnhandledRejectionHandler } from "./Utils";

registerUnhandledRejectionHandler();

describe("HttpConnection negotiate", () => {
    it("includes endpoint guidance if negotiate returns a 404 response", async () => {
        await VerifyLogger.run(async (logger) => {
            const options: IHttpConnectionOptions = {
                httpClient: new TestHttpClient()
                    .on("POST", () => new HttpResponse(404, "Not Found")),
                logger,
            };
            const connection = new HttpConnection("http://tempuri.org", options);

            await expect(connection.start(TransferFormat.Text))
                .rejects
                .toThrow("Either this is not a SignalR endpoint or there is a proxy blocking the connection.");
        },
        /Either this is not a SignalR endpoint or there is a proxy blocking the connection/);
    });

    it.each([
        [401, "Unauthorized", undefined, "Unauthorized"],
        [400, "Bad Request", "Some response from server", "Some response from server"],
    ])("preserves error details for a %s negotiate response", async (statusCode, statusText, content, expectedDetail) => {
        await VerifyLogger.run(async (logger) => {
            const response = content === undefined
                ? new HttpResponse(statusCode, statusText)
                : new HttpResponse(statusCode, statusText, content);
            const options: IHttpConnectionOptions = {
                httpClient: new TestHttpClient()
                    .on("POST", () => response),
                logger,
            };
            const connection = new HttpConnection("http://tempuri.org", options);

            const error = await connection.start(TransferFormat.Text).then(
                () => undefined,
                (reason) => reason as Error);

            expect(error).toBeInstanceOf(Error);
            expect(error!.message).toBe(
                `Failed to complete negotiation with the server: Error: ${expectedDetail}: Status code '${statusCode}'`);
            expect(error!.message).not.toContain("Either this is not a SignalR endpoint");
        },
        /Failed to complete negotiation with the server/);
    });
});
