// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { AbortError } from "./Errors";
import { FetchHttpClient } from "./FetchHttpClient";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";
import { Platform } from "./Utils";
import { XhrHttpClient } from "./XhrHttpClient";

/** Default implementation of {@link @microsoft/signalr.HttpClient}. */
export class DefaultHttpClient extends HttpClient {
    private readonly httpClient: HttpClient;

    /** Creates a new instance of the {@link @microsoft/signalr.DefaultHttpClient}, using the provided {@link @microsoft/signalr.ILogger} to log messages. */
    public constructor(logger: ILogger) {
        super();

        if (typeof fetch !== "undefined" || Platform.isNode) {
            this.httpClient = new FetchHttpClient(logger);
        } else if (typeof XMLHttpRequest !== "undefined") {
            this.httpClient = new XhrHttpClient(logger);
        } else {
            throw new Error("No usable HttpClient found.");
        }
    }

    /** @inheritDoc */
    public send(request: HttpRequest): Promise<HttpResponse> {
        // Check that abort was not signaled before calling send
        if (request.abortSignal && request.abortSignal.aborted) {
            return Promise.reject(new AbortError());
        }

        if (!request.method) {
            return Promise.reject(new Error("No method defined."));
        }
        if (!request.url) {
            return Promise.reject(new Error("No url defined."));
        }

        return this.httpClient.send(request);
    }

    public getCookieString(url: string): string {
        return this.httpClient.getCookieString(url);
    }
}
