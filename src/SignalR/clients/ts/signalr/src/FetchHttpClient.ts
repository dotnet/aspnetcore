// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// @ts-ignore: This will be removed from built files and is here to make the types available during dev work
import * as tough from "@types/tough-cookie";

import { AbortError, HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { Platform } from "./Utils";

export class FetchHttpClient extends HttpClient {
    private readonly abortControllerType: { prototype: AbortController, new(): AbortController };
    private readonly fetchType: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private readonly jar?: tough.CookieJar;

    private readonly logger: ILogger;

    public constructor(logger: ILogger) {
        super();
        this.logger = logger;

        if (typeof fetch === "undefined") {
            // In order to ignore the dynamic require in webpack builds we need to do this magic
            // @ts-ignore: TS doesn't know about these names
            const requireFunc = typeof __webpack_require__ === "function" ? __non_webpack_require__ : require;

            // Cookies aren't automatically handled in Node so we need to add a CookieJar to preserve cookies across requests
            this.jar = new (requireFunc("tough-cookie")).CookieJar();
            this.fetchType = requireFunc("node-fetch");

            // node-fetch doesn't have a nice API for getting and setting cookies
            // fetch-cookie will wrap a fetch implementation with a default CookieJar or a provided one
            this.fetchType = requireFunc("fetch-cookie")(this.fetchType, this.jar);

            // Node needs EventListener methods on AbortController which our custom polyfill doesn't provide
            this.abortControllerType = requireFunc("abort-controller");
        } else {
            this.fetchType = fetch.bind(self);
            this.abortControllerType = AbortController;
        }
    }

    /** @inheritDoc */
    public async send(request: HttpRequest): Promise<HttpResponse> {
        // Check that abort was not signaled before calling send
        if (request.abortSignal && request.abortSignal.aborted) {
            throw new AbortError();
        }

        if (!request.method) {
            throw new Error("No method defined.");
        }
        if (!request.url) {
            throw new Error("No url defined.");
        }

        const abortController = new this.abortControllerType();

        let error: any;
        // Hook our abortSignal into the abort controller
        if (request.abortSignal) {
            request.abortSignal.onabort = () => {
                abortController.abort();
                error = new AbortError();
            };
        }

        // If a timeout has been passed in, setup a timeout to call abort
        // Type needs to be any to fit window.setTimeout and NodeJS.setTimeout
        let timeoutId: any = null;
        if (request.timeout) {
            const msTimeout = request.timeout!;
            timeoutId = setTimeout(() => {
                abortController.abort();
                this.logger.log(LogLevel.Warning, `Timeout from HTTP request.`);
                error = new TimeoutError();
            }, msTimeout);
        }

        let response: Response;
        try {
            response = await this.fetchType(request.url!, {
                body: request.content!,
                cache: "no-cache",
                credentials: request.withCredentials === true ? "include" : "same-origin",
                headers: {
                    "Content-Type": "text/plain;charset=UTF-8",
                    "X-Requested-With": "XMLHttpRequest",
                    ...request.headers,
                },
                method: request.method!,
                mode: "cors",
                redirect: "manual",
                signal: abortController.signal,
            });
        } catch (e) {
            if (error) {
                throw error;
            }
            this.logger.log(
                LogLevel.Warning,
                `Error from HTTP request. ${e}.`,
            );
            throw e;
        } finally {
            if (timeoutId) {
                clearTimeout(timeoutId);
            }
            if (request.abortSignal) {
                request.abortSignal.onabort = null;
            }
        }

        if (!response.ok) {
            throw new HttpError(response.statusText, response.status);
        }

        const content = deserializeContent(response, request.responseType);
        const payload = await content;

        return new HttpResponse(
            response.status,
            response.statusText,
            payload,
        );
    }

    public getCookieString(url: string): string {
        let cookies: string = "";
        if (Platform.isNode && this.jar) {
            // @ts-ignore: unused variable
            this.jar.getCookies(url, (e, c) => cookies = c.join("; "));
        }
        return cookies;
    }
}

function deserializeContent(response: Response, responseType?: XMLHttpRequestResponseType): Promise<string | ArrayBuffer> {
    let content;
    switch (responseType) {
        case "arraybuffer":
            content = response.arrayBuffer();
            break;
        case "text":
            content = response.text();
            break;
        case "blob":
        case "document":
        case "json":
            throw new Error(`${responseType} is not supported.`);
        default:
            content = response.text();
            break;
    }

    return content;
}
