// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-console */

import { Logger, LogLevel } from './Logger';

export class NullLogger implements Logger {
  public static instance: Logger = new NullLogger();

  public log(_logLevel: LogLevel, _message: string): void { // eslint-disable-line @typescript-eslint/no-unused-vars
  }
}

export class ConsoleLogger implements Logger {
  private readonly minLevel: LogLevel;

  public constructor(minimumLogLevel: LogLevel) {
    this.minLevel = minimumLogLevel;
  }

  public log(logLevel: LogLevel, message: string | Error): void {
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
