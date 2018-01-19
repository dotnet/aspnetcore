// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient, HttpRequest, HttpResponse } from "../src/HttpClient"

type TestHttpHandlerResult = HttpResponse | string;
export type TestHttpHandler = (request: HttpRequest, next?: (request: HttpRequest) => Promise<HttpResponse>) => Promise<TestHttpHandlerResult> | TestHttpHandlerResult;

export class TestHttpClient extends HttpClient {
    private handler: (request: HttpRequest) => Promise<HttpResponse>;

    constructor() {
        super();
        this.handler = (request: HttpRequest) =>
            Promise.reject(`Request has no handler: ${request.method} ${request.url}`);

    }

    send(request: HttpRequest): Promise<HttpResponse> {
        return this.handler(request);
    }

    on(handler: TestHttpHandler): TestHttpClient;
    on(method: string | RegExp, handler: TestHttpHandler): TestHttpClient;
    on(method: string | RegExp, url: string, handler: TestHttpHandler): TestHttpClient;
    on(method: string | RegExp, url: RegExp, handler: TestHttpHandler): TestHttpClient;
    on(methodOrHandler: string | RegExp | TestHttpHandler, urlOrHandler?: string | RegExp | TestHttpHandler, handler?: TestHttpHandler): TestHttpClient {
        let method: string | RegExp;
        let url: string | RegExp;
        if ((typeof methodOrHandler === "string") || (methodOrHandler instanceof RegExp)) {
            method = methodOrHandler;
        }
        else if (methodOrHandler) {
            handler = methodOrHandler;
        }

        if ((typeof urlOrHandler === "string") || (urlOrHandler instanceof RegExp)) {
            url = urlOrHandler;
        }
        else if (urlOrHandler) {
            handler = urlOrHandler;
        }

        // TypeScript callers won't be able to do this, because TypeScript checks this for us.
        if (!handler) {
            throw new Error("Missing required argument: 'handler'");
        }

        let oldHandler = this.handler;
        let newHandler = async (request: HttpRequest) => {
            if (matches(method, request.method) && matches(url, request.url)) {
                let promise = handler(request, oldHandler);

                let val: TestHttpHandlerResult;
                if (promise instanceof Promise) {
                    val = await promise;
                } else {
                    val = promise;
                }

                if (typeof val === "string") {
                    return new HttpResponse(200, "OK", val);
                }
                else {
                    return val;
                }
            }
            else {
                return await oldHandler(request);
            }
        };
        this.handler = newHandler;

        return this;
    }
}

function matches(pattern: string | RegExp, actual: string): boolean {
    // Null or undefined pattern matches all.
    if (!pattern) {
        return true;
    }

    if (typeof pattern === "string") {
        return actual === pattern;
    }
    else {
        return pattern.test(actual);
    }
}
