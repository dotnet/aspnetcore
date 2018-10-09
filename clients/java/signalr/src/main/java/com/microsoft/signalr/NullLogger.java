// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class NullLogger implements Logger {
    @Override
    public void log(LogLevel logLevel, String message) { }

    @Override
    public void log(LogLevel logLevel, String formattedMessage, Object... args) { }
}
