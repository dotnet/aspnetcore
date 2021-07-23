// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.io.IOException;
import java.lang.reflect.Array;
import java.lang.reflect.GenericArrayType;
import java.lang.reflect.ParameterizedType;
import java.lang.reflect.Type;
import java.lang.reflect.TypeVariable;
import java.lang.reflect.WildcardType;
import java.nio.ByteBuffer;
import java.util.ArrayList;

class Utils {
    public static String appendQueryString(String original, String queryStringValue) {
        if (original.contains("?")) {
            return original + "&" + queryStringValue;
        } else {
            return  original + "?" + queryStringValue;
        }
    }

    public static Class<?> typeToClass(Type type) {
        if (type == null) {
            return null;
        }
        if (type instanceof Class) {
            return (Class<?>) type;
        } else if (type instanceof GenericArrayType) {
            // Instantiate an array of the same type as this type, then return its class
            return Array.newInstance(typeToClass(((GenericArrayType)type).getGenericComponentType()), 0).getClass();
        } else if (type instanceof ParameterizedType) {
            return typeToClass(((ParameterizedType) type).getRawType());
        } else if (type instanceof TypeVariable) {
            Type[] bounds = ((TypeVariable<?>) type).getBounds();
            return bounds.length == 0 ? Object.class : typeToClass(bounds[0]);
        } else if (type instanceof WildcardType) {
            Type[] bounds = ((WildcardType) type).getUpperBounds();
            return bounds.length == 0 ? Object.class : typeToClass(bounds[0]);
        } else {
            throw new UnsupportedOperationException("Cannot handle type class: " + type.getClass());
        }
    }

    @SuppressWarnings("unchecked")
    public static <T> T cast(Class<?> returnClass, Object obj) {
        // Primitive types can't be cast with the Class cast function
        if (returnClass.isPrimitive()) {
            return (T) obj;
        } else {
            return (T)returnClass.cast(obj);
        }
    }

    public static <T> T cast(Type returnType, Object obj) {
        return cast(typeToClass(returnType), obj);
    }
}