// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.List;

interface InvocationBinder {
    Class<?> getReturnType(String invocationId);
    List<Class<?>> getParameterTypes(String methodName);
}