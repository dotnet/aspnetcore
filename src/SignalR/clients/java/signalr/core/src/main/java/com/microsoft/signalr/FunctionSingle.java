// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import io.reactivex.rxjava3.core.Single;

/**
 * A callback that takes no parameters.
 */
public interface FunctionSingle<TResult> {
    // We can't use the @FunctionalInterface annotation because it's only
    // available on Android API Level 24 and above.
    Single<TResult> invoke();
}