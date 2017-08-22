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