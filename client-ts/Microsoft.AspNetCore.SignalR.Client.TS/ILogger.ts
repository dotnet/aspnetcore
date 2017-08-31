// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

export enum LogLevel {
    Information = 0,
    Warning,
    Error,
    None
}

export interface ILogger {
    log(logLevel: LogLevel, message: string): void;
}
