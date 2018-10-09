// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public enum LogLevel {
    Trace(0),
    Debug(1),
    Information(2),
    Warning(3),
    Error(4),
    Critical(5),
    None(6);

    public int value;
    LogLevel(int id) { this.value = id; }
}
