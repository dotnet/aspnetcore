// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { AbortError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";
import { XhrHttpClient } from "./XhrHttpClient";

let nodeHttpClientModule: any;
if (typeof XMLHttpRequest === "undefined") {
    // In order to ignore the dynamic require in webpack builds we need to do this magic
    // @ts-ignore: TS doesn't know about these names
    const requireFunc = typeof __webpack_require__ === "function" ? __non_webpack_require__ : require;
    nodeHttpClientModule = requireFunc("./NodeHttpClient");
}

/** Default implementation of {@link @aspnet/signalr.HttpClient}. */
export class DefaultHttpClient extends HttpClient {
    private readonly httpClient: HttpClient;

    /** Creates a new instance of the {@link @aspnet/signalr.DefaultHttpClient}, using the provided {@link @aspnet/signalr.ILogger} to log messages. */
    public constructor(logger: ILogger) {
        super();

        if (typeof XMLHttpRequest !== "undefined") {
            this.httpClient = new XhrHttpClient(logger);
        } else if (typeof nodeHttpClientModule !== "undefined") {
            this.httpClient = new nodeHttpClientModule.NodeHttpClient(logger);
        } else {
            throw new Error("No HttpClient could be created.");
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
