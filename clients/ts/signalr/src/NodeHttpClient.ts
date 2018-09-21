// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import * as http from "http";
import { URL } from "url";

import { AbortError, HttpError, TimeoutError } from "./Errors";
import { HttpClient, HttpRequest, HttpResponse } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { isArrayBuffer } from "./Utils";

export class NodeHttpClient extends HttpClient {
    private readonly logger: ILogger;

    public constructor(logger: ILogger) {
        super();
        this.logger = logger;
    }

    public send(request: HttpRequest): Promise<HttpResponse> {
        return new Promise<HttpResponse>((resolve, reject) => {
            const url = new URL(request.url!);
            const options: http.RequestOptions = {
                headers: {
                    // Tell auth middleware to 401 instead of redirecting
                    "X-Requested-With": "XMLHttpRequest",
                    ...request.headers,
                },
                hostname: url.hostname,
                method: request.method,
                // /abc/xyz + ?id=12ssa_30
                path: url.pathname + url.search,
                port: url.port,
            };

            const req = http.request(options, (res: http.IncomingMessage) => {
                const data: Buffer[] = [];
                let dataLength = 0;
                res.on("data", (chunk: any) => {
                    data.push(chunk);
                    // Buffer.concat will be slightly faster if we keep track of the length
                    dataLength += chunk.length;
                });

                res.on("end", () => {
                    if (request.abortSignal) {
                        request.abortSignal.onabort = null;
                    }

                    if (res.statusCode && res.statusCode >= 200 && res.statusCode < 300) {
                        let resp: string | ArrayBuffer;
                        if (request.responseType === "arraybuffer") {
                            resp = Buffer.concat(data, dataLength);
                            resolve(new HttpResponse(res.statusCode, res.statusMessage || "", resp));
                        } else {
                            resp = Buffer.concat(data, dataLength).toString();
                            resolve(new HttpResponse(res.statusCode, res.statusMessage || "", resp));
                        }
                    } else {
                        reject(new HttpError(res.statusMessage || "", res.statusCode || 0));
                    }
                });
            });

            if (request.abortSignal) {
                request.abortSignal.onabort = () => {
                    req.abort();
                    reject(new AbortError());
                };
            }

            if (request.timeout) {
                req.setTimeout(request.timeout, () => {
                    this.logger.log(LogLevel.Warning, `Timeout from HTTP request.`);
                    reject(new TimeoutError());
                });
            }

            req.on("error", (e) => {
                this.logger.log(LogLevel.Warning, `Error from HTTP request. ${e}`);
                reject(e);
            });

            if (isArrayBuffer(request.content)) {
                req.write(Buffer.from(request.content));
            } else {
                req.write(request.content || "");
            }
            req.end();
        });
    }
}
