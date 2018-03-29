import { ILogger, LogLevel } from "@aspnet/signalr";

export class TestLogger implements ILogger {
    public static instance: TestLogger = new TestLogger();

    public messages: Array<[LogLevel, string]> = [];

    public log(logLevel: LogLevel, message: string): void {
        this.messages.push([logLevel, message]);
    }

    public static getMessagesAndReset(): Array<[LogLevel, string]> {
        const messages = TestLogger.instance.messages;
        TestLogger.instance = new TestLogger();
        return messages;
    }
}
