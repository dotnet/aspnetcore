// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.Arrays;
import java.util.List;

class InvocationHandler {
    private final List<Type> types;
    private final Object action;

    InvocationHandler(Object action, Type... types) {
        this.action = action;
        this.types = Arrays.asList(types);
    }

    public List<Type> getTypes() {
        return types;
    }

    public Object getAction() {
        return action;
    }

    public boolean getHasResult() {
        return action instanceof FunctionBase;
    }
}