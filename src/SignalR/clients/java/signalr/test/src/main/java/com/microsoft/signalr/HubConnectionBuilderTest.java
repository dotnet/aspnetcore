// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import org.junit.jupiter.api.Test;

public class HubConnectionBuilderTest {
    @Test
    public void passingInNullToWithUrlThrows() {
        Throwable exception = assertThrows(IllegalArgumentException.class, () -> HubConnectionBuilder.create(null));
        assertEquals("A valid url is required.", exception.getMessage());
    }

    @Test
    public void passingInEmptyStringToWihtUrlThrows() {
        Throwable exception = assertThrows(IllegalArgumentException.class, () -> HubConnectionBuilder.create(""));
        assertEquals("A valid url is required.", exception.getMessage());
    }
}
