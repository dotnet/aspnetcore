// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.ClassCastException;
import java.lang.reflect.Type;
import java.lang.reflect.ParameterizedType;


/**
 * A utility for getting a Java Type from a literal generic Class.
 */
public abstract class TypeReference<T> {

    private final Type type;

    /**
     * Creates a new instance of {@link TypeReference}.
     *
     * This class implements Super Type Tokens (Gafter's Gadget) as a way to get a reference to generic types in
     * spite of type erasure since, sadly, {@code Foo<Bar>.class} is not valid Java.
     *
     * To get the Type of Class {@code Foo<Bar>}, use the following syntax:
     * <pre>{@code
     * Type fooBarType = (new TypeReference<Foo<Bar>>() { }).getType();
     * }</pre>
     *
     * To get the Type of class Foo, use a regular Type Token:
     * <pre>{@code
     * Type fooType = Foo.class;
     * }</pre>
     *
     *  @see <a href="http://gafter.blogspot.com/2006/12/super-type-tokens.html">Super Type Tokens</a>
     */
    public TypeReference() {
        try {
            this.type = ((ParameterizedType) getClass().getGenericSuperclass()).getActualTypeArguments()[0];
        } catch (ClassCastException ex) {
            throw new RuntimeException("TypeReference must be instantiated with a type parameter such as (new TypeReference<Foo<Bar>>() {}).");
        }
    }

    /**
     * Gets the referenced type.
     * @return The Type encapsulated by this TypeReference
     */
    public Type getType() {
        return this.type;
    }
}
