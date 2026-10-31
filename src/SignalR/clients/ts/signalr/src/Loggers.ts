// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ILogger, LogLevel } from "./ILogger";

/** A logger that does nothing when log messages are sent to it. */
export class NullLogger implements ILogger {
    /** The singleton instance of the {@link @microsoft/signalr.NullLogger}. */
    public static instance: ILogger = new NullLogger();

    private constructor() {}

    /** @inheritDoc */
    // eslint-disable-next-line
    public log(_logLevel: LogLevel, _message: string): void {
    }
}
