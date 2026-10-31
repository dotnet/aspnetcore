// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpClient, HttpRequest, HttpResponse } from "../src/HttpClient";

export type TestHttpHandlerResult = string | HttpResponse | any;
export type TestHttpHandler = (request: HttpRequest, next?: (request: HttpRequest) => Promise<HttpResponse>) => Promise<TestHttpHandlerResult> | TestHttpHandlerResult;

export class TestHttpClient extends HttpClient {
    private _handler: (request: HttpRequest) => Promise<HttpResponse>;
    public sentRequests: HttpRequest[];

    constructor() {
        super();
        this.sentRequests = [];
        this._handler = (request: HttpRequest) =>
            Promise.reject(`Request has no handler: ${request.method} ${request.url}`);

    }

    public send(request: HttpRequest): Promise<HttpResponse> {
        this.sentRequests.push(request);
        return this._handler(request);
    }

    public on(handler: TestHttpHandler): TestHttpClient;
    public on(method: string | RegExp, handler: TestHttpHandler): TestHttpClient;
    public on(method: string | RegExp, url: string, handler: TestHttpHandler): TestHttpClient;
    public on(method: string | RegExp, url: RegExp, handler: TestHttpHandler): TestHttpClient;
    public on(methodOrHandler: string | RegExp | TestHttpHandler, urlOrHandler?: string | RegExp | TestHttpHandler, handler?: TestHttpHandler): TestHttpClient {
        let method: string | RegExp;
        let url: string | RegExp;
        if ((typeof methodOrHandler === "string") || (methodOrHandler instanceof RegExp)) {
            method = methodOrHandler;
        } else if (methodOrHandler) {
            handler = methodOrHandler;
        }

        if ((typeof urlOrHandler === "string") || (urlOrHandler instanceof RegExp)) {
            url = urlOrHandler;
        } else if (urlOrHandler) {
            handler = urlOrHandler;
        }

        // TypeScript callers won't be able to do this, because TypeScript checks this for us.
        if (!handler) {
            throw new Error("Missing required argument: 'handler'");
        }

        const oldHandler = this._handler;
        const newHandler = async (request: HttpRequest) => {
            if (matches(method, request.method!) && matches(url, request.url!)) {
                const promise = handler!(request, oldHandler);

                let val: TestHttpHandlerResult;
                if (promise instanceof Promise) {
                    val = await promise;
                } else {
                    val = promise;
                }

                if (typeof val === "string") {
                    // string payload
                    return new HttpResponse(200, "OK", val);
                } else if (typeof val === "object" && val.statusCode) {
                    // HttpResponse payload
                    return val as HttpResponse;
                } else {
                    // JSON payload
                    return new HttpResponse(200, "OK", JSON.stringify(val));
                }
            } else {
                return await oldHandler(request);
            }
        };
        this._handler = newHandler;

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
    } else {
        return pattern.test(actual);
    }
}
