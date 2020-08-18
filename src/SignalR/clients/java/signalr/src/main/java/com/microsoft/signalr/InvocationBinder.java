// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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