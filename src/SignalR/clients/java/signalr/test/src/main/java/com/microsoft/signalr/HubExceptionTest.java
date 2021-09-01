// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;

import org.junit.jupiter.api.Test;

class HubExceptionTest {
    @Test
    public void VeryHubExceptionMesssageIsSet() {
        String errorMessage = "This is a HubException";
        HubException hubException = new HubException(errorMessage);
        assertEquals(hubException.getMessage(), errorMessage);
    }

    @Test
    public void VeryHubExceptionInnerExceptionIsSet() {
        String errorMessage = "This is the inner exception of the HubException";
        Exception innerException = new Exception(errorMessage);
        HubException hubException = new HubException(null, innerException);
        assertEquals(hubException.getCause().getMessage(), errorMessage);
    }
}
