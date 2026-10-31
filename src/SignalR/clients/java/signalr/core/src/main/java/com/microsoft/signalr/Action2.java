// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

/**
 * A callback that takes two parameters.
 *
 * @param <T1> The type of the first parameter to the callback.
 * @param <T2> The type of the second parameter to the callback.
 */
public interface Action2<T1, T2> {
    // We can't use the @FunctionalInterface annotation because it's only
    // available on Android API Level 24 and above.
    void invoke(T1 param1, T2 param2);
}
