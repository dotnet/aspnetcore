import { ILogger, LogLevel } from "@microsoft/signalr";

// Since JavaScript modules are file-based, we can just pull in utilities from the
// main library directly even if they aren't exported.
import { ConsoleLogger } from "@microsoft/signalr/dist/esm/Utils";

export class TestLog {
    public messages: [Date, LogLevel, string][] = [];

    public addMessage(timestamp: Date, logLevel: LogLevel, message: string): void {
        this.messages.push([timestamp, logLevel, message]);
    }

    public getLog(): string {
        // Dump the logs to a string
        let str = "";
        for (const [timestamp, level, message] of this.messages) {
            str += `[${timestamp.toISOString()}] ${LogLevel[level]}: ${message}\r\n`;
        }

        return str;
    }

    public getLogUrl(): string {
        const log = this.getLog();
        return `data:text/plain;base64,${escape(btoa(log))}`;
    }

    public open(): void {
        window.open(this.getLogUrl());
    }
}

export class TestLogger implements ILogger {
    public static instance: TestLogger = new TestLogger();
    private static _consoleLogger: ConsoleLogger = new ConsoleLogger(LogLevel.Trace);

    public currentLog: TestLog = new TestLog();

    public log(logLevel: LogLevel, message: string): void {
        this.currentLog.addMessage(new Date(), logLevel, message);

        // Also write to browser console
        TestLogger._consoleLogger.log(logLevel, message);
    }

    public static saveLogsAndReset(): TestLog {
        const currentLog = TestLogger.instance.currentLog;
        TestLogger.instance.currentLog = new TestLog();
        return currentLog;
    }
}
