// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ILogger, LogLevel } from "./ILogger"

export class NullLogger implements ILogger {
    log(logLevel: LogLevel, message: string): void {
    }
}

export class ConsoleLogger implements ILogger {
    private readonly minimumLogLevel: LogLevel;

    constructor(minimumLogLevel: LogLevel) {
        this.minimumLogLevel = minimumLogLevel;
    }

    log(logLevel: LogLevel, message: string): void {
        if (logLevel >= this.minimumLogLevel) {
            console.log(`${LogLevel[logLevel]}: ${message}`);
        }
    }
}

export namespace LoggerFactory {
    export function createLogger(logging?: ILogger | LogLevel) {
        if (logging === undefined) {
            return new ConsoleLogger(LogLevel.Information);
        }

        if (logging === null) {
            return new NullLogger();
        }

        if ((<ILogger>logging).log) {
            return <ILogger>logging;
        }

        return new ConsoleLogger(<LogLevel>logging);
    }
}