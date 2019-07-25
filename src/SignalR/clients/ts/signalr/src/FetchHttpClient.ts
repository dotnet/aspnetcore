// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { AbortError, HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";

export class FetchHttpClient extends HttpClient {
    private readonly logger: ILogger;

    public constructor(logger: ILogger) {
        super();
        this.logger = logger;
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

        return new Promise<HttpResponse>((resolve, reject) => {
            const abortController = new AbortController();

            const fetchRequest = new Request(request.url!, {
                body: request.content!,
                cache: "no-cache",
                credentials: "include",
                headers: {
                    "Content-Type": "text/plain;charset=UTF-8",
                    "X-Requested-With": "Fetch",
                    ...request.headers,
                },
                method: request.method!,
                mode: "cors",
                signal: abortController.signal,
            });

            // Hook our abourtSignal into the abort controller
            if (request.abortSignal) {
                request.abortSignal.onabort = () => {
                    abortController.abort();
                    reject(new AbortError());
                };
            }

            // If a timeout has been passed in setup a timeout to call abort
            // Type needs to be any to fit window.setTimeout and NodeJS.setTimeout
            let timeoutId: any = null;
            if (request.timeout) {
                const msTimeout = request.timeout!;
                timeoutId = setTimeout(() => {
                    abortController.abort();
                    reject(new TimeoutError());
                }, msTimeout);
            }

            fetch(fetchRequest)
                .then((response: Response) => {
                    if (timeoutId) {
                        clearTimeout(timeoutId);
                    }
                    if (!response.ok) {
                        throw new Error(`${response.status}: ${response.statusText}.`);
                    } else {
                        return response;
                    }
                })
                .then((response: Response) => {
                    if (request.abortSignal) {
                        request.abortSignal.onabort = null;
                    }

                    const content = deserializeContent(response, request.responseType);

                    content.then((payload) => {
                        resolve(new HttpResponse(
                            response.status,
                            response.statusText,
                            payload,
                        ));
                    }).catch(() => {
                        reject(new HttpError(response.statusText, response.status));
                    });
                })
                .catch((error) => {
                    this.logger.log(
                        LogLevel.Warning,
                        `Error from HTTP request. ${error.message}.`,
                    );
                    const [statusText, status] = error.message.split(":");
                    reject(new HttpError(statusText, status));
                });
        });
    }
}

function deserializeContent(response: Response, responseType?: XMLHttpRequestResponseType): Promise<any> {
    let content;
    switch (responseType) {
        case "arraybuffer":
            content = response.arrayBuffer();
            break;
        case "blob":
            content = response.blob();
            break;
        case "document":
            content = response.json();
            break;
        case "json":
            content = response.json();
            break;
        case "text":
            content = response.text();
            break;
        default:
            content = response.text();
            break;
    }

    return content;
}
