// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

class TestBinder implements InvocationBinder {
    private Type[] paramTypes = null;
    private Type returnType = null;

    public TestBinder(Type[] paramTypes, Type returnType) {
        this.paramTypes = paramTypes;
        this.returnType = returnType;
    }

    @Override
    public Type getReturnType(String invocationId) {
        return returnType;
    }

    @Override
    public List<Type> getParameterTypes(String methodName) {
        if (paramTypes == null) {
            return new ArrayList<>();
        }
        return new ArrayList<Type>(Arrays.asList(paramTypes));
    }
}