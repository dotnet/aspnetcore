import { HttpTransportType } from "./ITransport";
/** Error thrown when an HTTP request fails. */
export declare class HttpError extends Error {
    private __proto__;
    /** The HTTP status code represented by this error. */
    statusCode: number;
    /** Constructs a new instance of {@link @microsoft/signalr.HttpError}.
     *
     * @param {string} errorMessage A descriptive error message.
     * @param {number} statusCode The HTTP status code represented by this error.
     */
    constructor(errorMessage: string, statusCode: number);
}
/** Error thrown when a timeout elapses. */
export declare class TimeoutError extends Error {
    private __proto__;
    /** Constructs a new instance of {@link @microsoft/signalr.TimeoutError}.
     *
     * @param {string} errorMessage A descriptive error message.
     */
    constructor(errorMessage?: string);
}
/** Error thrown when an action is aborted. */
export declare class AbortError extends Error {
    private __proto__;
    /** Constructs a new instance of {@link AbortError}.
     *
     * @param {string} errorMessage A descriptive error message.
     */
    constructor(errorMessage?: string);
}
/** Error thrown when the selected transport is unsupported by the browser. */
/** @private */
export declare class UnsupportedTransportError extends Error {
    private __proto__;
    /** The {@link @microsoft/signalr.HttpTransportType} this error occurred on. */
    transport: HttpTransportType;
    /** The type name of this error. */
    errorType: string;
    /** Constructs a new instance of {@link @microsoft/signalr.UnsupportedTransportError}.
     *
     * @param {string} message A descriptive error message.
     * @param {HttpTransportType} transport The {@link @microsoft/signalr.HttpTransportType} this error occurred on.
     */
    constructor(message: string, transport: HttpTransportType);
}
/** Error thrown when the selected transport is disabled by the browser. */
/** @private */
export declare class DisabledTransportError extends Error {
    private __proto__;
    /** The {@link @microsoft/signalr.HttpTransportType} this error occurred on. */
    transport: HttpTransportType;
    /** The type name of this error. */
    errorType: string;
    /** Constructs a new instance of {@link @microsoft/signalr.DisabledTransportError}.
     *
     * @param {string} message A descriptive error message.
     * @param {HttpTransportType} transport The {@link @microsoft/signalr.HttpTransportType} this error occurred on.
     */
    constructor(message: string, transport: HttpTransportType);
}
/** Error thrown when the selected transport cannot be started. */
/** @private */
export declare class FailedToStartTransportError extends Error {
    private __proto__;
    /** The {@link @microsoft/signalr.HttpTransportType} this error occurred on. */
    transport: HttpTransportType;
    /** The type name of this error. */
    errorType: string;
    /** Constructs a new instance of {@link @microsoft/signalr.FailedToStartTransportError}.
     *
     * @param {string} message A descriptive error message.
     * @param {HttpTransportType} transport The {@link @microsoft/signalr.HttpTransportType} this error occurred on.
     */
    constructor(message: string, transport: HttpTransportType);
}
/** Error thrown when the negotiation with the server failed to complete. */
/** @private */
export declare class FailedToNegotiateWithServerError extends Error {
    private __proto__;
    /** The type name of this error. */
    errorType: string;
    /** Constructs a new instance of {@link @microsoft/signalr.FailedToNegotiateWithServerError}.
     *
     * @param {string} message A descriptive error message.
     */
    constructor(message: string);
}
/** Error thrown when multiple errors have occurred. */
/** @private */
export declare class AggregateErrors extends Error {
    private __proto__;
    /** The collection of errors this error is aggregating. */
    innerErrors: Error[];
    /** Constructs a new instance of {@link @microsoft/signalr.AggregateErrors}.
     *
     * @param {string} message A descriptive error message.
     * @param {Error[]} innerErrors The collection of errors this error is aggregating.
     */
    constructor(message: string, innerErrors: Error[]);
}
//# sourceMappingURL=Errors.d.ts.map