// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import io.reactivex.rxjava3.core.Single;

/**
 * A callback that takes one parameter.
 *
 * @param <T1> The type of the first parameter to the callback.
 * @param <T2> The type of the second parameter to the callback.
 * @param <T3> The type of the third parameter to the callback.
 */
public interface Function3Single<T1, T2, T3, TResult> {
    // We can't use the @FunctionalInterface annotation because it's only
    // available on Android API Level 24 and above.
    Single<TResult> invoke(T1 param1, T2 param2, T3 param3);
}