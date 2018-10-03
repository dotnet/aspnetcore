// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class HttpConnectionOptionsTest {
    @Test
    public void CheckHttpConnectionOptionsFields() {
        Transport mockTransport = new MockTransport();
        HttpConnectionOptions options = new HttpConnectionOptions(mockTransport, LogLevel.Information, true);
        assertEquals(LogLevel.Information, options.getLoglevel());
        assertTrue(options.getSkipNegotiate());
        assertNotNull(options.getTransport());
    }
}
