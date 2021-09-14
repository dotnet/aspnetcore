// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { HttpTransportType } from "./ITransport";

/** Error thrown when an HTTP request fails. */
export class HttpError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** The HTTP status code represented by this error. */
    public statusCode: number;

    /** Constructs a new instance of {@link @microsoft/signalr.HttpError}.
     *
     * @param {string} errorMessage A descriptive error message.
     * @param {number} statusCode The HTTP status code represented by this error.
     */
    constructor(errorMessage: string, statusCode: number) {
        const trueProto = new.target.prototype;
        super(`${errorMessage}: Status code '${statusCode}'`);
        this.statusCode = statusCode;

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when a timeout elapses. */
export class TimeoutError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** Constructs a new instance of {@link @microsoft/signalr.TimeoutError}.
     *
     * @param {string} errorMessage A descriptive error message.
     */
    constructor(errorMessage: string = "A timeout occurred.") {
        const trueProto = new.target.prototype;
        super(errorMessage);

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when an action is aborted. */
export class AbortError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** Constructs a new instance of {@link AbortError}.
     *
     * @param {string} errorMessage A descriptive error message.
     */
    constructor(errorMessage: string = "An abort occurred.") {
        const trueProto = new.target.prototype;
        super(errorMessage);

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when the selected transport is unsupported by the browser. */
/** @private */
export class UnsupportedTransportError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** The {@link @microsoft/signalr.HttpTransportType} this error occured on. */
    public transport: HttpTransportType;

    /** The type name of this error. */
    public errorType: string;

    /** Constructs a new instance of {@link @microsoft/signalr.UnsupportedTransportError}.
     *
     * @param {string} message A descriptive error message.
     * @param {HttpTransportType} transport The {@link @microsoft/signalr.HttpTransportType} this error occured on.
     */
    constructor(message: string, transport: HttpTransportType) {
        const trueProto = new.target.prototype;
        super(message);
        this.transport = transport;
        this.errorType = 'UnsupportedTransportError';

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when the selected transport is disabled by the browser. */
/** @private */
export class DisabledTransportError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** The {@link @microsoft/signalr.HttpTransportType} this error occured on. */
    public transport: HttpTransportType;

    /** The type name of this error. */
    public errorType: string;

    /** Constructs a new instance of {@link @microsoft/signalr.DisabledTransportError}.
     *
     * @param {string} message A descriptive error message.
     * @param {HttpTransportType} transport The {@link @microsoft/signalr.HttpTransportType} this error occured on.
     */
    constructor(message: string, transport: HttpTransportType) {
        const trueProto = new.target.prototype;
        super(message);
        this.transport = transport;
        this.errorType = 'DisabledTransportError';

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when the selected transport cannot be started. */
/** @private */
export class FailedToStartTransportError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** The {@link @microsoft/signalr.HttpTransportType} this error occured on. */
    public transport: HttpTransportType;

    /** The type name of this error. */
    public errorType: string;

    /** Constructs a new instance of {@link @microsoft/signalr.FailedToStartTransportError}.
     *
     * @param {string} message A descriptive error message.
     * @param {HttpTransportType} transport The {@link @microsoft/signalr.HttpTransportType} this error occured on.
     */
    constructor(message: string, transport: HttpTransportType) {
        const trueProto = new.target.prototype;
        super(message);
        this.transport = transport;
        this.errorType = 'FailedToStartTransportError';

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when the negotiation with the server failed to complete. */
/** @private */
export class FailedToNegotiateWithServerError extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** The type name of this error. */
    public errorType: string;

    /** Constructs a new instance of {@link @microsoft/signalr.FailedToNegotiateWithServerError}.
     *
     * @param {string} message A descriptive error message.
     */
    constructor(message: string) {
        const trueProto = new.target.prototype;
        super(message);
        this.errorType = 'FailedToNegotiateWithServerError';

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when multiple errors have occured. */
/** @private */
export class AggregateErrors extends Error {
    // @ts-ignore: Intentionally unused.
    // eslint-disable-next-line @typescript-eslint/naming-convention
    private __proto__: Error;

    /** The collection of errors this error is aggregating. */
    public innerErrors: Error[];

    /** Constructs a new instance of {@link @microsoft/signalr.AggregateErrors}.
     *
     * @param {string} message A descriptive error message.
     * @param {Error[]} innerErrors The collection of errors this error is aggregating.
     */
    constructor(message: string, innerErrors: Error[]) {
        const trueProto = new.target.prototype;
        super(message);

        this.innerErrors = innerErrors;

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}
