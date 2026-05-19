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
 * @param <T4> The type of the fourth parameter to the callback.
 * @param <T5> The type of the fifth parameter to the callback.
 * @param <T6> The type of the sixth parameter to the callback.
 * @param <T7> The type of the seventh parameter to the callback.
 */
public interface Function7Single<T1, T2, T3, T4, T5, T6, T7, TResult> {
    // We can't use the @FunctionalInterface annotation because it's only
    // available on Android API Level 24 and above.
    Single<TResult> invoke(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7);
}