import { ConsoleLogger, ILogger, LogLevel } from "@aspnet/signalr";

export class TestLog {
    public messages: Array<[Date, LogLevel, string]> = [];

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
    private static consoleLogger: ConsoleLogger = new ConsoleLogger(LogLevel.Trace);

    public currentLog: TestLog = new TestLog();

    public log(logLevel: LogLevel, message: string): void {
        this.currentLog.addMessage(new Date(), logLevel, message);

        // Also write to browser console
        TestLogger.consoleLogger.log(logLevel, message);
    }

    public static saveLogsAndReset(testName: string): TestLog {
        const currentLog = TestLogger.instance.currentLog;

        // Stash the messages in a global to help people review them
        if (window) {
            const win = window as any;
            if (!win.TestLogMessages) {
                win.TestLogMessages = {};
            }
            win.TestLogMessages[testName] = currentLog;
        }

        TestLogger.instance.currentLog = new TestLog();
        return currentLog;
    }
}
