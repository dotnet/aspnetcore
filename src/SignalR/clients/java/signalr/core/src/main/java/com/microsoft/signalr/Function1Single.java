// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import io.reactivex.rxjava3.core.Single;

/**
 * A callback that takes one parameter.
 *
 * @param <T1> The type of the first parameter to the callback.
 */
public interface Function1Single<T1, TResult> {
    // We can't use the @FunctionalInterface annotation because it's only
    // available on Android API Level 24 and above.
    Single<TResult> invoke(T1 param1);
}