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

    public void assertLog(String logMessage) {
        boolean foundLog = false;
        for (ILoggingEvent log : appender.list) {
            if (log.getFormattedMessage().contentEquals(logMessage)) {
                foundLog = true;
                break;
            }
        }

        assertTrue(foundLog, String.format("Log message '%s' not found", logMessage));
    }

    @Override
    public void close() {
        this.logger.detachAppender(this.appender);
    }

}