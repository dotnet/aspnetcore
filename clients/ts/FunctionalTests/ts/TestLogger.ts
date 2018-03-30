import { ConsoleLogger, ILogger, LogLevel } from "@aspnet/signalr";

export class TestLogger implements ILogger {
    public static instance: TestLogger = new TestLogger();
    private static consoleLogger: ConsoleLogger = new ConsoleLogger(LogLevel.Trace);

    public messages: Array<[LogLevel, string]> = [];

    public log(logLevel: LogLevel, message: string): void {
        // Capture log message so it can be reported later
        this.messages.push([logLevel, message]);

        // Also write to browser console
        TestLogger.consoleLogger.log(logLevel, message);
    }

    public static getMessagesAndReset(): Array<[LogLevel, string]> {
        const messages = TestLogger.instance.messages;
        TestLogger.instance = new TestLogger();
        return messages;
    }
}
