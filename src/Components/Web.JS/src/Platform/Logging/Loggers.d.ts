import { Logger, LogLevel } from './Logger';
export declare class NullLogger implements Logger {
    static instance: Logger;
    log(_logLevel: LogLevel, _message: string): void;
}
export declare class ConsoleLogger implements Logger {
    private readonly minLevel;
    constructor(minimumLogLevel: LogLevel);
    log(logLevel: LogLevel, message: string | Error): void;
}
