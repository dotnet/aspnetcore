// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// These values are designed to match the ASP.NET Log Levels since that's the pattern we're emulating here.
export enum LogLevel {
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6,
}

export interface ILogger {
    log(logLevel: LogLevel, message: string): void;
}
