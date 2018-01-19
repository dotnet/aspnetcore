// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { LoggerFactory } from "../src/Loggers"
import { ILogger, LogLevel } from "../src/ILogger"

describe("LoggerFactory", () => {
    it("creates ConsoleLogger when no logging specified", () => {
        expect(LoggerFactory.createLogger().constructor.name).toBe("ConsoleLogger");
    });

    it("creates NullLogger when logging is set to null", () => {
        expect(LoggerFactory.createLogger(null).constructor.name).toBe("NullLogger");
    });

    it("creates ConsoleLogger when log level specified", () => {
        expect(LoggerFactory.createLogger(LogLevel.Information).constructor.name).toBe("ConsoleLogger");
    });

    it("does not create its own logger if the user provides one", () => {
        let customLogger : ILogger = { log: LogLevel => {} };
        expect(LoggerFactory.createLogger(customLogger)).toBe(customLogger);
    });
});