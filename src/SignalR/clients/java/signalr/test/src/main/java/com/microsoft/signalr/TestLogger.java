// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertTrue;

import org.slf4j.LoggerFactory;

import ch.qos.logback.classic.Logger;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.read.ListAppender;

class TestLogger implements AutoCloseable {
    private Logger logger;
    private ListAppender<ILoggingEvent> appender;

    public TestLogger() {
        this("com.microsoft.signalr.HubConnection");
    }

    public TestLogger(String category) {
        this.logger = (Logger)LoggerFactory.getLogger(category);
        this.appender = new ListAppender<ILoggingEvent>();
        this.appender.start();
        this.logger.addAppender(this.appender);
    }

    public ILoggingEvent assertLog(String logMessage) {
        // Copy items just in case logs are written while iterating
        ILoggingEvent[] list = appender.list.toArray(new ILoggingEvent[1]);
        for (ILoggingEvent log : list) {
            if (log.getFormattedMessage().startsWith(logMessage)) {
                return log;
            }
        }

        assertTrue(false, String.format("Log message '%s' not found", logMessage));
        return null;
    }

    @Override
    public void close() {
        this.logger.detachAppender(this.appender);
    }

}