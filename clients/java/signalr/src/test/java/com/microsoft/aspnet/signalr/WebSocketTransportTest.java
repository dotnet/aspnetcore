// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.HashMap;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;

import org.junit.jupiter.api.Test;

class WebSocketTransportTest {
    @Test
    public void WebsocketThrowsIfItCantConnect() throws Exception {
        Transport transport = new WebSocketTransport(new HashMap<>(), new DefaultHttpClient(new NullLogger()), new NullLogger());
        ExecutionException exception = assertThrows(ExecutionException.class, () -> transport.start("http://www.example.com").get(1, TimeUnit.SECONDS));
        assertEquals("There was an error starting the Websockets transport.", exception.getCause().getMessage());
    }
}
