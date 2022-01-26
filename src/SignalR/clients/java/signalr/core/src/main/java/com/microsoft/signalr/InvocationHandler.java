// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.Arrays;
import java.util.List;

class InvocationHandler {
    private final List<Type> types;
    private final Object action;
    private final Boolean hasResult;

    InvocationHandler(FunctionBase action, Type... types) {
        this.action = action;
        this.types = Arrays.asList(types);
        this.hasResult = false;
    }

    InvocationHandler(ActionBase action, Type... types) {
        this.action = action;
        this.types = Arrays.asList(types);
        this.hasResult = true;
    }

    public List<Type> getTypes() {
        return types;
    }

    public FunctionBase getAction() {
        return (FunctionBase)action;
    }
}