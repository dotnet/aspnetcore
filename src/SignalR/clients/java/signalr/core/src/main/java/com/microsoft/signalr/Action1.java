// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

/**
 * A callback that takes one parameter.
 *
 * @param <T1> The type of the first parameter to the callback.
 */
public interface Action1<T1> {
    // We can't use the @FunctionalInterface annotation because it's only
    // available on Android API Level 24 and above.
    void invoke(T1 param1);
}
