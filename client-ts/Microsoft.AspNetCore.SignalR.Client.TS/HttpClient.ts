// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpError } from "./HttpError"

export interface IHttpClient {
    get(url: string, headers?: Map<string, string>): Promise<string>;
    options(url: string, headers?: Map<string, string>): Promise<string>;
    post(url: string, content: string, headers?: Map<string, string>): Promise<string>;
}

export class HttpClient implements IHttpClient {
    get(url: string, headers?: Map<string, string>): Promise<string> {
        return this.xhr("GET", url, headers);
    }

    options(url: string, headers?: Map<string, string>): Promise<string> {
        return this.xhr("OPTIONS", url, headers);
    }

    post(url: string, content: string, headers?: Map<string, string>): Promise<string> {
        return this.xhr("POST", url, headers, content);
    }

    private xhr(method: string, url: string, headers?: Map<string, string>, content?: string): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            let xhr = new XMLHttpRequest();

            xhr.open(method, url, true);
            xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            if (headers) {
                headers.forEach((value, header) => xhr.setRequestHeader(header, value));
            }

            xhr.send(content);
            xhr.onload = () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve(xhr.response || xhr.responseText);
                }
                else {
                    reject(new HttpError(xhr.statusText, xhr.status));
                }
            };

            xhr.onerror = () => {
                reject(new HttpError(xhr.statusText, xhr.status));
            }
        });
    }
}