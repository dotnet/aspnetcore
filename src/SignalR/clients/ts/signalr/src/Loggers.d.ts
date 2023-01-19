import { ILogger, LogLevel } from "./ILogger";
/** A logger that does nothing when log messages are sent to it. */
export declare class NullLogger implements ILogger {
    /** The singleton instance of the {@link @microsoft/signalr.NullLogger}. */
    static instance: ILogger;
    private constructor();
    /** @inheritDoc */
    log(_logLevel: LogLevel, _message: string): void;
}
//# sourceMappingURL=Loggers.d.ts.map