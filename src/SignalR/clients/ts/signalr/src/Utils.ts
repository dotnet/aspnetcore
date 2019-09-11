// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { NullLogger } from "./Loggers";
import { IStreamResult, IStreamSubscriber, ISubscription } from "./Stream";

/** @private */
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

/** @private */
export function getDataDetail(data: any, includeContent: boolean): string {
    let detail = "";
    if (isArrayBuffer(data)) {
        detail = `Binary data of length ${data.byteLength}`;
        if (includeContent) {
            detail += `. Content: '${formatArrayBuffer(data)}'`;
        }
    } else if (typeof data === "string") {
        detail = `String data of length ${data.length}`;
        if (includeContent) {
            detail += `. Content: '${data}'`;
        }
    }
    return detail;
}

/** @private */
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

// Also in signalr-protocol-msgpack/Utils.ts
/** @private */
export function isArrayBuffer(val: any): val is ArrayBuffer {
    return val && typeof ArrayBuffer !== "undefined" &&
        (val instanceof ArrayBuffer ||
        // Sometimes we get an ArrayBuffer that doesn't satisfy instanceof
        (val.constructor && val.constructor.name === "ArrayBuffer"));
}

/** @private */
export async function sendMessage(logger: ILogger, transportName: string, httpClient: HttpClient, url: string, accessTokenFactory: (() => string | Promise<string>) | undefined, content: string | ArrayBuffer, logMessageContent: boolean): Promise<void> {
    let headers;
    if (accessTokenFactory) {
        const token = await accessTokenFactory();
        if (token) {
            headers = {
                ["Authorization"]: `Bearer ${token}`,
            };
        }
    }

    logger.log(LogLevel.Trace, `(${transportName} transport) sending data. ${getDataDetail(content, logMessageContent)}.`);

    const responseType = isArrayBuffer(content) ? "arraybuffer" : "text";
    const response = await httpClient.post(url, {
        content,
        headers,
        responseType,
    });

    logger.log(LogLevel.Trace, `(${transportName} transport) request complete. Response status: ${response.statusCode}.`);
}

/** @private */
export function createLogger(logger?: ILogger | LogLevel) {
    if (logger === undefined) {
        return new ConsoleLogger(LogLevel.Information);
    }

    if (logger === null) {
        return NullLogger.instance;
    }

    if ((logger as ILogger).log) {
        return logger as ILogger;
    }

    return new ConsoleLogger(logger as LogLevel);
}

/** @private */
export class Subject<T> implements IStreamResult<T> {
    public observers: Array<IStreamSubscriber<T>>;
    public cancelCallback?: () => Promise<void>;

    constructor() {
        this.observers = [];
    }

    public next(item: T): void {
        for (const observer of this.observers) {
            observer.next(item);
        }
    }

    public error(err: any): void {
        for (const observer of this.observers) {
            if (observer.error) {
                observer.error(err);
            }
        }
    }

    public complete(): void {
        for (const observer of this.observers) {
            if (observer.complete) {
                observer.complete();
            }
        }
    }

    public subscribe(observer: IStreamSubscriber<T>): ISubscription<T> {
        this.observers.push(observer);
        return new SubjectSubscription(this, observer);
    }
}

/** @private */
export class SubjectSubscription<T> implements ISubscription<T> {
    private subject: Subject<T>;
    private observer: IStreamSubscriber<T>;

    constructor(subject: Subject<T>, observer: IStreamSubscriber<T>) {
        this.subject = subject;
        this.observer = observer;
    }

    public dispose(): void {
        const index: number = this.subject.observers.indexOf(this.observer);
        if (index > -1) {
            this.subject.observers.splice(index, 1);
        }

        if (this.subject.observers.length === 0 && this.subject.cancelCallback) {
            this.subject.cancelCallback().catch((_) => { });
        }
    }
}

/** @private */
export class ConsoleLogger implements ILogger {
    private readonly minimumLogLevel: LogLevel;

    constructor(minimumLogLevel: LogLevel) {
        this.minimumLogLevel = minimumLogLevel;
    }

    public log(logLevel: LogLevel, message: string): void {
        if (logLevel >= this.minimumLogLevel) {
            switch (logLevel) {
                case LogLevel.Critical:
                case LogLevel.Error:
                    console.error(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
                    break;
                case LogLevel.Warning:
                    console.warn(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
                    break;
                case LogLevel.Information:
                    console.info(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
                    break;
                default:
                    // console.debug only goes to attached debuggers in Node, so we use console.log for Trace and Debug
                    console.log(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
                    break;
            }
        }
    }
}
