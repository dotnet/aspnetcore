// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// @ts-ignore: This will be removed from built files and is here to make the types available during dev work
import { CookieJar } from "@types/tough-cookie";

import { AbortError, HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { Platform, getGlobalThis, isArrayBuffer } from "./Utils";

export class FetchHttpClient extends HttpClient {
    private readonly _abortControllerType: { prototype: AbortController, new(): AbortController };
    private readonly _fetchType: (input: RequestInfo, init?: RequestInit) => Promise<Response>;
    private readonly _jar?: CookieJar;

    private readonly _logger: ILogger;

    public constructor(logger: ILogger) {
        super();
        this._logger = logger;

        if (typeof fetch === "undefined") {
            // In order to ignore the dynamic require in webpack builds we need to do this magic
            // @ts-ignore: TS doesn't know about these names
            const requireFunc = typeof __webpack_require__ === "function" ? __non_webpack_require__ : require;

            // Cookies aren't automatically handled in Node so we need to add a CookieJar to preserve cookies across requests
            this._jar = new (requireFunc("tough-cookie")).CookieJar();
            this._fetchType = requireFunc("node-fetch");

            // node-fetch doesn't have a nice API for getting and setting cookies
            // fetch-cookie will wrap a fetch implementation with a default CookieJar or a provided one
            this._fetchType = requireFunc("fetch-cookie")(this._fetchType, this._jar);
        } else {
            this._fetchType = fetch.bind(getGlobalThis());
        }
        if (typeof AbortController === "undefined") {
            // In order to ignore the dynamic require in webpack builds we need to do this magic
            // @ts-ignore: TS doesn't know about these names
            const requireFunc = typeof __webpack_require__ === "function" ? __non_webpack_require__ : require;

            // Node needs EventListener methods on AbortController which our custom polyfill doesn't provide
            this._abortControllerType = requireFunc("abort-controller");
        } else {
            this._abortControllerType = AbortController;
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

        const abortController = new this._abortControllerType();

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
                this._logger.log(LogLevel.Warning, `Timeout from HTTP request.`);
                error = new TimeoutError();
            }, msTimeout);
        }

        if (request.content === "") {
            request.content = undefined;
        }
        if (request.content) {
            // Explicitly setting the Content-Type header for React Native on Android platform.
            request.headers = request.headers || {};
            if (isArrayBuffer(request.content)) {
                request.headers["Content-Type"] = "application/octet-stream";
            } else {
                request.headers["Content-Type"] = "text/plain;charset=UTF-8";
            }
        }

        let response: Response;
        try {
            response = await this._fetchType(request.url!, {
                body: request.content,
                cache: "no-cache",
                credentials: request.withCredentials === true ? "include" : "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    ...request.headers,
                },
                method: request.method!,
                mode: "cors",
                redirect: "follow",
                signal: abortController.signal,
            });
        } catch (e) {
            if (error) {
                throw error;
            }
            this._logger.log(
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
            const errorMessage = await deserializeContent(response, "text") as string;
            throw new HttpError(errorMessage || response.statusText, response.status);
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
        if (Platform.isNode && this._jar) {
            // @ts-ignore: unused variable
            this._jar.getCookies(url, (e, c) => cookies = c.join("; "));
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
