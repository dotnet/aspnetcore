// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.lang.reflect.ParameterizedType;

public class TypeReference<T> {

    private final Type type;

    /**
     * Creates a new instance of {@link TypeReference}.
     *
     * To get the Type of Class Foo, use the following syntax:
     * <pre>{@code
     * Type fooType = (new TypeReference<Foo>() { }).getType();
     * </pre>
     */
    public TypeReference() {
        Type superclass = getClass().getGenericSuperclass();
        if (superclass instanceof Class) {
            throw new RuntimeException("Missing type parameter.");
        }
        this.type = ((ParameterizedType) superclass).getActualTypeArguments()[0];
    }

    /**
     * Gets the referenced type.
     */ 
    public Type getType() {
        return this.type;
    }
}