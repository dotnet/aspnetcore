package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.lang.reflect.ParameterizedType;

public class TypeReference<T> {

    private final Type type;

    protected TypeReference() {
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