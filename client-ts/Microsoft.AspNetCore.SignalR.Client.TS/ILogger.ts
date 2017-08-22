export enum LogLevel {
    Information = 0,
    Warning,
    Error,
    None
}

export interface ILogger {
    log(logLevel: LogLevel, message: string): void;
}