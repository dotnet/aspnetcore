/* eslint-disable no-console */

import { ILogger, LogLevel } from './ILogger';

export class NullLogger implements ILogger {
  public static instance: ILogger = new NullLogger();

  private constructor() { }

  public log(_logLevel: LogLevel, _message: string): void { // eslint-disable-line @typescript-eslint/no-unused-vars
  }
}

export class ConsoleLogger implements ILogger {
  private readonly minimumLogLevel: LogLevel;

  public constructor(minimumLogLevel: LogLevel) {
    this.minimumLogLevel = minimumLogLevel;
  }

  public log(logLevel: LogLevel, message: string | Error): void {
    if (logLevel >= this.minimumLogLevel) {
      switch (logLevel) {
        case LogLevel.Critical:
        case LogLevel.Error:
          console.error(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
          break;
        case LogLevel.Warning:
          console.warn(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
          break;
        case LogLevel.Information:
          console.info(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
          break;
        default:
          // console.debug only goes to attached debuggers in Node, so we use console.log for Trace and Debug
          console.log(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
          break;
      }
    }
  }
}
