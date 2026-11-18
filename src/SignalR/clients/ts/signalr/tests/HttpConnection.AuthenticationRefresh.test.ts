// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HeaderNames } from "../src/HeaderNames";
import { HttpConnection, INegotiateResponse } from "../src/HttpConnection";
import { HttpResponse } from "../src/HttpClient";
import { IHttpConnectionOptions } from "../src/IHttpConnectionOptions";
import { HttpTransportType, TransferFormat } from "../src/ITransport";
import { VerifyLogger } from "./Common";
import { TestHttpClient } from "./TestHttpClient";
import { PromiseSource, registerUnhandledRejectionHandler } from "./Utils";

const connectionToken = "123abc";
const negotiateResponse: INegotiateResponse = {
    availableTransports: [
        { transport: "LongPolling", transferFormats: ["Text", "Binary"] },
    ],
    connectionId: "abc123",
    connectionToken,
    negotiateVersion: 1,
};

registerUnhandledRejectionHandler();

describe("HttpConnection authentication refresh", () => {
    it("posts authentication refresh and retries a challenged Long Polling poll", async () => {
        await VerifyLogger.run(async (logger) => {
            let pollCount = 0;
            let refreshCount = 0;
            let refreshConnectionTokenHeader: string | undefined;
            const closed = new PromiseSource();
            let closeError: Error | undefined;
            const httpClient = new TestHttpClient()
                .on("POST", /\/negotiate/, () => negotiateResponse)
                .on("POST", /\/refresh/, (request) => {
                    refreshCount++;
                    refreshConnectionTokenHeader = request.headers![HeaderNames.ConnectionToken];
                    return { tokenLifetimeSeconds: 42 };
                })
                .on("GET", () => {
                    pollCount++;
                    switch (pollCount) {
                        case 1:
                            return new HttpResponse(200);
                        case 2:
                            return new HttpResponse(409, "Conflict", JSON.stringify({ error: "authentication_refresh_required" }));
                        case 3:
                            return new HttpResponse(204);
                        default:
                            throw new Error(`Unexpected poll ${pollCount}.`);
                    }
                })
                .on("DELETE", () => new HttpResponse(202));
            const options: IHttpConnectionOptions = {
                httpClient,
                logger,
                transport: HttpTransportType.LongPolling,
            };
            const connection = new HttpConnection("http://tempuri.org/chat", options);
            connection.onclose = (error) => {
                closeError = error;
                closed.resolve();
            };

            try {
                await connection.start(TransferFormat.Text);
                await closed;

                expect(closeError).toBeUndefined();
                expect(refreshCount).toBe(1);
                expect(refreshConnectionTokenHeader).toBe(connectionToken);
                expect(pollCount).toBe(3);
            } finally {
                await connection.stop();
            }
        });
    });
});
