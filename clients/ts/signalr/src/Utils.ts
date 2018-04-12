// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";

export class Arg {
    public static isRequired(val: any, name: string): void {
        if (val === null || val === undefined) {
            throw new Error(`The '${name}' argument is required.`);
        }
    }

    public static isIn(val: any, values: any, name: string): void {
        // TypeScript enums have keys for **both** the name and the value of each enum member on the type itself.
        if (!(val in values)) {
            throw new Error(`Unknown ${name} value: ${val}.`);
        }
    }
}

export function getDataDetail(data: any, includeContent: boolean): string {
    let length: string = null;
    if (data instanceof ArrayBuffer) {
        length = `Binary data of length ${data.byteLength}`;
        if (includeContent) {
            length += `. Content: '${formatArrayBuffer(data)}'`;
        }
    } else if (typeof data === "string") {
        length = `String data of length ${data.length}`;
        if (includeContent) {
            length += `. Content: '${data}'.`;
        }
    }
    return length;
}

export function formatArrayBuffer(data: ArrayBuffer): string {
    const view = new Uint8Array(data);

    // Uint8Array.map only supports returning another Uint8Array?
    let str = "";
    view.forEach((num) => {
        const pad = num < 16 ? "0" : "";
        str += `0x${pad}${num.toString(16)} `;
    });

    // Trim of trailing space.
    return str.substr(0, str.length - 1);
}

export async function sendMessage(logger: ILogger, transportName: string, httpClient: HttpClient, url: string, accessTokenFactory: () => string | Promise<string>, content: string | ArrayBuffer, logMessageContent: boolean): Promise<void> {
    let headers;
    const token = await accessTokenFactory();
    if (token) {
        headers = {
            ["Authorization"]: `Bearer ${token}`,
        };
    }

    logger.log(LogLevel.Trace, `(${transportName} transport) sending data. ${getDataDetail(content, logMessageContent)}.`);

    const response = await httpClient.post(url, {
        content,
        headers,
    });

    logger.log(LogLevel.Trace, `(${transportName} transport) request complete. Response status: ${response.statusCode}.`);
}
