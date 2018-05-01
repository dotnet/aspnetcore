// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ILogger, LogLevel } from "./ILogger";

/** A logger that does nothing when log messages are sent to it. */
export class NullLogger implements ILogger {
    /** The singleton instance of the {@link NullLogger}. */
    public static instance: ILogger = new NullLogger();

    private constructor() {}

    /** @inheritDoc */
    public log(logLevel: LogLevel, message: string): void {
    }
}
