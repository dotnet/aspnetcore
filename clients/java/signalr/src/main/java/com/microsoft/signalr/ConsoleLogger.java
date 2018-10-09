// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;

public class ConsoleLogger implements Logger {
    private LogLevel logLevel;
    private DateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mmZ");

    public ConsoleLogger(LogLevel logLevel) {
            this.logLevel = logLevel;
    }

    @Override
    public void log(LogLevel logLevel, String message) {
        if (logLevel.value >= this.logLevel.value) {
            String timeStamp = dateFormat.format(new Date());
            message = String.format("[%s] [%s] %s", timeStamp, logLevel, message);
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
            String timeStamp = dateFormat.format(new Date());
            formattedMessage = String.format("[%s] [%s] %s%n", timeStamp, logLevel, formattedMessage);
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
