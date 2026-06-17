// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { EOL } from "os";
import { ILogger, LogLevel } from "../src/ILogger";
import { HttpTransportType } from "../src/ITransport";

export function eachTransport(action: (transport: HttpTransportType) => void): void {
    const transportTypes = [
        HttpTransportType.WebSockets,
        HttpTransportType.ServerSentEvents,
        HttpTransportType.LongPolling ];
    transportTypes.forEach((t) => action(t));
}

export function eachEndpointUrl(action: (givenUrl: string, expectedUrl: string) => void): void {
    const urls = [
        [ "http://tempuri.org/endpoint/?q=my/Data", "http://tempuri.org/endpoint/negotiate?q=my%2FData&negotiateVersion=1" ],
        [ "http://tempuri.org/endpoint?q=my/Data", "http://tempuri.org/endpoint/negotiate?q=my%2FData&negotiateVersion=1" ],
        [ "http://tempuri.org/endpoint", "http://tempuri.org/endpoint/negotiate?negotiateVersion=1" ],
        [ "http://tempuri.org/endpoint/", "http://tempuri.org/endpoint/negotiate?negotiateVersion=1" ],
    ];

    urls.forEach((t) => action(t[0], t[1]));
}

type ErrorMatchFunction = (error: string) => boolean;

export class VerifyLogger implements ILogger {
    public unexpectedErrors: string[];
    private _expectedErrors: ErrorMatchFunction[];

    public constructor(...expectedErrors: (RegExp | string | ErrorMatchFunction)[]) {
        this.unexpectedErrors = [];
        this._expectedErrors = [];
        expectedErrors.forEach((element) => {
            if (element instanceof RegExp) {
                this._expectedErrors.push((e) => element.test(e));
            } else if (typeof element === "string") {
                this._expectedErrors.push((e) => element === e);
            } else {
                this._expectedErrors.push(element);
            }
        }, this);
    }

    public static async run(fn: (logger: VerifyLogger) => Promise<void>, ...expectedErrors: (RegExp | string | ErrorMatchFunction)[]): Promise<void> {
        const logger = new VerifyLogger(...expectedErrors);
        await fn(logger);
        expect(logger.unexpectedErrors.join(EOL)).toBe("");
    }

    public log(logLevel: LogLevel, message: string): void {
        if (logLevel >= LogLevel.Error) {
            if (!this._expectedErrors.some((fn) => fn(message))) {
                this.unexpectedErrors.push(message);
            }
        }
    }
}
