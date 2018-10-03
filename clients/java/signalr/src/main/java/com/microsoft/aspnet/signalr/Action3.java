// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

@FunctionalInterface
public interface Action3<T1, T2, T3> {
    void invoke(T1 param1, T2 param2, T3 param3);
}
