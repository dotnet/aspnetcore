// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import org.junit.jupiter.api.Test;

public class HubConnectionBuilderTest {
    @Test
    public void callingBuildWithoutCallingWithUrlThrows() {
        HubConnectionBuilder builder = new HubConnectionBuilder();
        Throwable exception = assertThrows(RuntimeException.class, () -> builder.build());
        assertEquals("The \'HubConnectionBuilder.withUrl\' method must be called before building the connection.", exception.getMessage());
    }

    @Test
    public void passingInNullToWithUrlThrows() {
        HubConnectionBuilder builder = new HubConnectionBuilder();
        Throwable exception = assertThrows(IllegalArgumentException.class, () -> builder.withUrl(null));
        assertEquals("A valid url is required.", exception.getMessage());
    }

    @Test
    public void passingInEmptyStringToWihtUrlThrows() {
        HubConnectionBuilder builder = new HubConnectionBuilder();
        Throwable exception = assertThrows(IllegalArgumentException.class, () -> builder.withUrl(""));
        assertEquals("A valid url is required.", exception.getMessage());
    }
}
