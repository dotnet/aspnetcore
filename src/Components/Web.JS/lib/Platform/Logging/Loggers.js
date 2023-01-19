// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* eslint-disable no-console */
import { LogLevel } from './Logger';
export class NullLogger {
    log(_logLevel, _message) {
    }
}
NullLogger.instance = new NullLogger();
export class ConsoleLogger {
    constructor(minimumLogLevel) {
        this.minLevel = minimumLogLevel;
    }
    log(logLevel, message) {
        if (logLevel >= this.minLevel) {
            const msg = `[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`;
            switch (logLevel) {
                case LogLevel.Critical:
                case LogLevel.Error:
                    console.error(msg);
                    break;
                case LogLevel.Warning:
                    console.warn(msg);
                    break;
                case LogLevel.Information:
                    console.info(msg);
                    break;
                default:
                    // console.debug only goes to attached debuggers in Node, so we use console.log for Trace and Debug
                    console.log(msg);
                    break;
            }
        }
    }
}
//# sourceMappingURL=Loggers.js.map