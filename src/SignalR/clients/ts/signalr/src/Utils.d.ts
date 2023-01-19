import { HttpClient } from "./HttpClient";
import { ILogger, LogLevel } from "./ILogger";
import { IStreamSubscriber, ISubscription } from "./Stream";
import { Subject } from "./Subject";
import { IHttpConnectionOptions } from "./IHttpConnectionOptions";
/** The version of the SignalR client. */
export declare const VERSION: string;
/** @private */
export declare class Arg {
    static isRequired(val: any, name: string): void;
    static isNotEmpty(val: string, name: string): void;
    static isIn(val: any, values: any, name: string): void;
}
/** @private */
export declare class Platform {
    static get isBrowser(): boolean;
    static get isWebWorker(): boolean;
    static get isReactNative(): boolean;
    static get isNode(): boolean;
}
/** @private */
export declare function getDataDetail(data: any, includeContent: boolean): string;
/** @private */
export declare function formatArrayBuffer(data: ArrayBuffer): string;
/** @private */
export declare function isArrayBuffer(val: any): val is ArrayBuffer;
/** @private */
export declare function sendMessage(logger: ILogger, transportName: string, httpClient: HttpClient, url: string, content: string | ArrayBuffer, options: IHttpConnectionOptions): Promise<void>;
/** @private */
export declare function createLogger(logger?: ILogger | LogLevel): ILogger;
/** @private */
export declare class SubjectSubscription<T> implements ISubscription<T> {
    private _subject;
    private _observer;
    constructor(subject: Subject<T>, observer: IStreamSubscriber<T>);
    dispose(): void;
}
/** @private */
export declare class ConsoleLogger implements ILogger {
    private readonly _minLevel;
    out: {
        error(message: any): void;
        warn(message: any): void;
        info(message: any): void;
        log(message: any): void;
    };
    constructor(minimumLogLevel: LogLevel);
    log(logLevel: LogLevel, message: string): void;
}
/** @private */
export declare function getUserAgentHeader(): [string, string];
/** @private */
export declare function constructUserAgent(version: string, os: string, runtime: string, runtimeVersion: string | undefined): string;
/** @private */
export declare function getErrorString(e: any): string;
/** @private */
export declare function getGlobalThis(): unknown;
//# sourceMappingURL=Utils.d.ts.map