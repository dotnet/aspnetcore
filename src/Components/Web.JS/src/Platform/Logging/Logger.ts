// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These values are designed to match the ASP.NET Log Levels since that's the pattern we're emulating here.
/** Indicates the severity of a log message.
 *
 * Log Levels are ordered in increasing severity. So `Debug` is more severe than `Trace`, etc.
 */
export enum LogLevel {
  /** Log level for very low severity diagnostic messages. */
  Trace = 0,
  /** Log level for low severity diagnostic messages. */
  Debug = 1,
  /** Log level for informational diagnostic messages. */
  Information = 2,
  /** Log level for diagnostic messages that indicate a non-fatal problem. */
  Warning = 3,
  /** Log level for diagnostic messages that indicate a failure in the current operation. */
  Error = 4,
  /** Log level for diagnostic messages that indicate a failure that will terminate the entire application. */
  Critical = 5,
  /** The highest possible log level. Used when configuring logging to indicate that no log messages should be emitted. */
  None = 6,
}

/** An abstraction that provides a sink for diagnostic messages. */
export interface Logger {
  /** Called by the framework to emit a diagnostic message.
   *
   * @param {LogLevel} logLevel The severity level of the message.
   * @param {string} message The message.
   */
  log(logLevel: LogLevel, message: string | Error): void;
}
