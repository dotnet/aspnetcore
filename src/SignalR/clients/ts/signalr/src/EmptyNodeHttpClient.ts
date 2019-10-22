// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This is an empty implementation of the NodeHttpClient that will be included in browser builds so the output file will be smaller

import { HttpClient, HttpResponse } from "./HttpClient";
import { ILogger } from "./ILogger";

/** @private */
export class NodeHttpClient extends HttpClient {
    // @ts-ignore: Need ILogger to compile, but unused variables generate errors
    public constructor(logger: ILogger) {
        super();
    }

    public send(): Promise<HttpResponse> {
        return Promise.reject(new Error("If using Node either provide an XmlHttpRequest polyfill or consume the cjs or esm script instead of the browser/signalr.js one."));
    }
}
