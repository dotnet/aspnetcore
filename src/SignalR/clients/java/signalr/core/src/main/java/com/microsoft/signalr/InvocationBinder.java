// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.List;

/**
 * An abstraction for passing around information about method signatures.
 */
public interface InvocationBinder {
    Type getReturnType(String invocationId);
    List<Type> getParameterTypes(String methodName);
}