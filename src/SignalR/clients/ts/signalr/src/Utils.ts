// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { NullLogger } from "./Loggers";
import { IStreamSubscriber, ISubscription } from "./Stream";
import { Subject } from "./Subject";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
import { VERSION } from "./pkg-version";

// Version token that will be replaced by the prepack command
/** The version of the SignalR client. */

export { VERSION };
/** @private */
export class Arg {
    public static isRequired(val: any, name: string): void {
        if (val === null || val === undefined) {
            throw new Error(`The '${name}' argument is required.`);
        }
    }
    public static isNotEmpty(val: string, name: string): void {
        if (!val || val.match(/^\s*$/)) {
            throw new Error(`The '${name}' argument should not be empty.`);
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
export class Platform {
    // react-native has a window but no document so we should check both
    public static get isBrowser(): boolean {
        return !Platform.isNode && typeof window === "object" && typeof window.document === "object";
    }

    // WebWorkers don't have a window object so the isBrowser check would fail
    public static get isWebWorker(): boolean {
        return !Platform.isNode && typeof self === "object" && "importScripts" in self;
    }

    // react-native has a window but no document
    static get isReactNative(): boolean {
        return !Platform.isNode && typeof window === "object" && typeof window.document === "undefined";
    }

    // Node apps shouldn't have a window object, but WebWorkers don't either
    // so we need to check for both WebWorker and window
    public static get isNode(): boolean {
        return typeof process !== "undefined" && process.release && process.release.name === "node";
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
export async function sendMessage(logger: ILogger, transportName: string, httpClient: HttpClient, url: string,
                                  content: string | ArrayBuffer, options: IHttpConnectionOptions): Promise<void> {
    const headers: {[k: string]: string} = {};

    const [name, value] = getUserAgentHeader();
    headers[name] = value;

    logger.log(LogLevel.Trace, `(${transportName} transport) sending data. ${getDataDetail(content, options.logMessageContent!)}.`);

    const responseType = isArrayBuffer(content) ? "arraybuffer" : "text";
    const response = await httpClient.post(url, {
        content,
        headers: { ...headers, ...options.headers},
        responseType,
        timeout: options.timeout,
        withCredentials: options.withCredentials,
    });

    logger.log(LogLevel.Trace, `(${transportName} transport) request complete. Response status: ${response.statusCode}.`);
}

/** @private */
export function createLogger(logger?: ILogger | LogLevel): ILogger {
    if (logger === undefined) {
        return new ConsoleLogger(LogLevel.Information);
    }

    if (logger === null) {
        return NullLogger.instance;
    }

    if ((logger as ILogger).log !== undefined) {
        return logger as ILogger;
    }

    return new ConsoleLogger(logger as LogLevel);
}

/** @private */
export class SubjectSubscription<T> implements ISubscription<T> {
    private _subject: Subject<T>;
    private _observer: IStreamSubscriber<T>;

    constructor(subject: Subject<T>, observer: IStreamSubscriber<T>) {
        this._subject = subject;
        this._observer = observer;
    }

    public dispose(): void {
        const index: number = this._subject.observers.indexOf(this._observer);
        if (index > -1) {
            this._subject.observers.splice(index, 1);
        }

        if (this._subject.observers.length === 0 && this._subject.cancelCallback) {
            this._subject.cancelCallback().catch((_) => { });
        }
    }
}

/** @private */
export class ConsoleLogger implements ILogger {
    private readonly _minLevel: LogLevel;

    // Public for testing purposes.
    public out: {
        error(message: any): void,
        warn(message: any): void,
        info(message: any): void,
        log(message: any): void,
    };

    constructor(minimumLogLevel: LogLevel) {
        this._minLevel = minimumLogLevel;
        this.out = console;
    }

    public log(logLevel: LogLevel, message: string): void {
        if (logLevel >= this._minLevel) {
            const msg = `[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`;
            switch (logLevel) {
                case LogLevel.Critical:
                case LogLevel.Error:
                    this.out.error(msg);
                    break;
                case LogLevel.Warning:
                    this.out.warn(msg);
                    break;
                case LogLevel.Information:
                    this.out.info(msg);
                    break;
                default:
                    // console.debug only goes to attached debuggers in Node, so we use console.log for Trace and Debug
                    this.out.log(msg);
                    break;
            }
        }
    }
}

/** @private */
export function getUserAgentHeader(): [string, string] {
    let userAgentHeaderName = "X-SignalR-User-Agent";
    if (Platform.isNode) {
        userAgentHeaderName = "User-Agent";
    }
    return [ userAgentHeaderName, constructUserAgent(VERSION, getOsName(), getRuntime(), getRuntimeVersion()) ];
}

/** @private */
export function constructUserAgent(version: string, os: string, runtime: string, runtimeVersion: string | undefined): string {
    // Microsoft SignalR/[Version] ([Detailed Version]; [Operating System]; [Runtime]; [Runtime Version])
    let userAgent: string = "Microsoft SignalR/";

    const majorAndMinor = version.split(".");
    userAgent += `${majorAndMinor[0]}.${majorAndMinor[1]}`;
    userAgent += ` (${version}; `;

    if (os && os !== "") {
        userAgent += `${os}; `;
    } else {
        userAgent += "Unknown OS; ";
    }

    userAgent += `${runtime}`;

    if (runtimeVersion) {
        userAgent += `; ${runtimeVersion}`;
    } else {
        userAgent += "; Unknown Runtime Version";
    }

    userAgent += ")";
    return userAgent;
}

// eslint-disable-next-line spaced-comment
/*#__PURE__*/ function getOsName(): string {
    if (Platform.isNode) {
        switch (process.platform) {
            case "win32":
                return "Windows NT";
            case "darwin":
                return "macOS";
            case "linux":
                return "Linux";
            default:
                return process.platform;
        }
    } else {
        return "";
    }
}

// eslint-disable-next-line spaced-comment
/*#__PURE__*/ function getRuntimeVersion(): string | undefined {
    if (Platform.isNode) {
        return process.versions.node;
    }
    return undefined;
}

function getRuntime(): string {
    if (Platform.isNode) {
        return "NodeJS";
    } else {
        return "Browser";
    }
}

/** @private */
export function getErrorString(e: any): string {
    if (e.stack) {
        return e.stack;
    } else if (e.message) {
        return e.message;
    }
    return `${e}`;
}

/** @private */
export function getGlobalThis(): unknown {
    // globalThis is semi-new and not available in Node until v12
    if (typeof globalThis !== "undefined") {
        return globalThis;
    }
    if (typeof self !== "undefined") {
        return self;
    }
    if (typeof window !== "undefined") {
        return window;
    }
    if (typeof global !== "undefined") {
        return global;
    }
    throw new Error("could not find global");
}
