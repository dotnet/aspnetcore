// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ILogger, LogLevel } from "./ILogger";

export class NullLogger implements ILogger {
    public static instance: ILogger = new NullLogger();

    private constructor() {}

    public log(logLevel: LogLevel, message: string): void {
    }
}
