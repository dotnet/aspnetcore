// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

import org.slf4j.LoggerFactory;

import ch.qos.logback.classic.Logger;
import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.Appender;
import ch.qos.logback.core.AppenderBase;

class TestLogger implements AutoCloseable {
    private Logger logger;
    private Lock lock = new ReentrantLock();
    private List<ILoggingEvent> list = new ArrayList<ILoggingEvent>();
    private Appender<ILoggingEvent> appender;

    public TestLogger() {
        this("com.microsoft.signalr.HubConnection");
    }

    public TestLogger(String category) {
        this.logger = (Logger)LoggerFactory.getLogger(category);
        this.appender = new AppenderBase<ILoggingEvent>() {
            public void append(ILoggingEvent event) {
                lock.lock();
                try {
                    list.add(event);
                } finally {
                    lock.unlock();
                }
            }
        };
        this.appender.start();
        this.logger.addAppender(this.appender);
    }

    public ILoggingEvent[] getLogs() {
        lock.lock();
        try {
            return list.toArray(new ILoggingEvent[0]);
        } finally {
            lock.unlock();
        }
    }

    public ILoggingEvent assertLog(String logMessage) {
        ILoggingEvent[] localList;
        lock.lock();
        try {
            localList = list.toArray(new ILoggingEvent[0]);
        } finally {
            lock.unlock();
        }

        for (ILoggingEvent log : localList) {
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