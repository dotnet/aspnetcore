// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.HashMap;
import java.util.concurrent.atomic.AtomicBoolean;

import org.junit.jupiter.api.Test;

class WebSocketTransportTest {
    @Test
    public void CanPassNullExitCodeToOnClosed() {
        WebSocketTransport transport = new WebSocketTransport(new HashMap<>(), new WebSocketTestHttpClient());
        AtomicBoolean closed = new AtomicBoolean();
        transport.setOnClose(reason -> {
            closed.set(true);
        });
        transport.start("");
        transport.stop();
        assertTrue(closed.get());
    }
}
