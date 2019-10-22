// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// @ts-ignore: This will be removed from built files and is here to make the types available during dev work
import * as Request from "@types/request";

import { AbortError, HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { isArrayBuffer } from "./Utils";

let requestModule: Request.RequestAPI<Request.Request, Request.CoreOptions, Request.RequiredUriUrl>;
if (typeof XMLHttpRequest === "undefined") {
    // In order to ignore the dynamic require in webpack builds we need to do this magic
    // @ts-ignore: TS doesn't know about these names
    const requireFunc = typeof __webpack_require__ === "function" ? __non_webpack_require__ : require;
    requestModule = requireFunc("request");
}

/** @private */
export class NodeHttpClient extends HttpClient {
    private readonly logger: ILogger;
    private readonly request: typeof requestModule;
    private readonly cookieJar: Request.CookieJar;

    public constructor(logger: ILogger) {
        super();
        if (typeof requestModule === "undefined") {
            throw new Error("The 'request' module could not be loaded.");
        }

        this.logger = logger;
        this.cookieJar = requestModule.jar();
        this.request = requestModule.defaults({ jar: this.cookieJar });
    }

    public send(httpRequest: HttpRequest): Promise<HttpResponse> {
        return new Promise<HttpResponse>((resolve, reject) => {

            let requestBody: Buffer | string;
            if (isArrayBuffer(httpRequest.content)) {
                requestBody = Buffer.from(httpRequest.content);
            } else {
                requestBody = httpRequest.content || "";
            }

            const currentRequest = this.request(httpRequest.url!, {
                body: requestBody,
                // If binary is expected 'null' should be used, otherwise for text 'utf8'
                encoding: httpRequest.responseType === "arraybuffer" ? null : "utf8",
                headers: {
                    // Tell auth middleware to 401 instead of redirecting
                    "X-Requested-With": "XMLHttpRequest",
                    ...httpRequest.headers,
                },
                method: httpRequest.method,
                timeout: httpRequest.timeout,
            },
            (error, response, body) => {
                if (httpRequest.abortSignal) {
                    httpRequest.abortSignal.onabort = null;
                }

                if (error) {
                    if (error.code === "ETIMEDOUT") {
                        this.logger.log(LogLevel.Warning, `Timeout from HTTP request.`);
                        reject(new TimeoutError());
                    }
                    this.logger.log(LogLevel.Warning, `Error from HTTP request. ${error}`);
                    reject(error);
                    return;
                }

                if (response.statusCode >= 200 && response.statusCode < 300) {
                    resolve(new HttpResponse(response.statusCode, response.statusMessage || "", body));
                } else {
                    reject(new HttpError(response.statusMessage || "", response.statusCode || 0));
                }
            });

            if (httpRequest.abortSignal) {
                httpRequest.abortSignal.onabort = () => {
                    currentRequest.abort();
                    reject(new AbortError());
                };
            }
        });
    }

    public getCookieString(url: string): string {
        return this.cookieJar.getCookieString(url);
    }
}
