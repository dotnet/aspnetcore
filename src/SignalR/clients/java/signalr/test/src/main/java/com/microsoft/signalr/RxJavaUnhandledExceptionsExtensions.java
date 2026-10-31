// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;

import org.junit.jupiter.api.extension.AfterAllCallback;
import org.junit.jupiter.api.extension.BeforeAllCallback;
import org.junit.jupiter.api.extension.ExtensionContext;

import io.reactivex.rxjava3.plugins.RxJavaPlugins;

// Use by adding "@ExtendWith({RxJavaUnhandledExceptionsExtensions.class})" to a test class
class RxJavaUnhandledExceptionsExtensions implements BeforeAllCallback, AfterAllCallback {
    private final BlockingQueue<Throwable> errors = new LinkedBlockingQueue<Throwable>();

    @Override
    public void beforeAll(final ExtensionContext context) {
        RxJavaPlugins.setErrorHandler(error -> {
            errors.put(error);
        });
    }

    @Override
    public void afterAll(final ExtensionContext context) {
        if (errors.size() != 0) {
            String RxErrors = "";
            for (final Throwable throwable : errors) {
                StringWriter stringWriter = new StringWriter();
                PrintWriter printWriter = new PrintWriter(stringWriter);
                throwable.printStackTrace(printWriter);
                RxErrors += String.format("%s\n", stringWriter.toString());
            }
            throw new RuntimeException(RxErrors);
        }
    }
}