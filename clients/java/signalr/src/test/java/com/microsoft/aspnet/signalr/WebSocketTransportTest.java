// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.concurrent.TimeUnit;

import org.junit.jupiter.api.Test;

public class WebSocketTransportTest {
    @Test
    public void WebsocketThrowsIfItCantConnect() throws Exception {
        Transport transport = new WebSocketTransport("www.notarealurl12345.fake", new NullLogger());
        Throwable exception = assertThrows(Exception.class, () -> transport.start().get(1,TimeUnit.SECONDS));
        assertEquals("There was an error starting the Websockets transport.", exception.getCause().getMessage());
    }
}
