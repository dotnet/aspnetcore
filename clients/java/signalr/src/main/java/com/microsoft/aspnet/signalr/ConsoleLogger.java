// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

public class ConsoleLogger implements Logger {
    private LogLevel logLevel;
    public ConsoleLogger(LogLevel logLevel) {
            this.logLevel = logLevel;
    }

    @Override
    public void log(LogLevel logLevel, String message) {
        if(logLevel.value >= this.logLevel.value){
            switch (logLevel) {
                case Debug:
                case Information:
                    System.out.println(message);
                    break;
                case Warning:
                case Error:
                case Critical:
                    System.err.println(message);
                    break;
            }
        }
    }

    @Override
    public void log(LogLevel logLevel, String formattedMessage, Object... args) {
        if (logLevel.value >= this.logLevel.value) {
            formattedMessage = formattedMessage + "%n";
            switch (logLevel) {
                case Debug:
                case Information:
                    System.out.printf(formattedMessage, args);
                    break;
                case Warning:
                case Error:
                case Critical:
                    System.err.printf(formattedMessage, args);
                    break;
            }
        }
    }
}
