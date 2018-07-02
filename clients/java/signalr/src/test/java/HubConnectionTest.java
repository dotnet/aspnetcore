// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import com.google.gson.JsonArray;
import org.junit.Test;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

import static org.junit.Assert.*;

public class HubConnectionTest {
    @Test
    public void checkHubConnectionState() throws InterruptedException {
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);
        hubConnection.start();
        assertTrue(hubConnection.connected);

        hubConnection.stop();
        assertFalse(hubConnection.connected);
    }

    @Test
    public void SendWithNoParamsTriggersOnHandler() throws InterruptedException {
        AtomicInteger value = new AtomicInteger(0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        Action callback = (param) -> {
            assertEquals(0, value.get());
            value.incrementAndGet();
        };
        hubConnection.On("inc", callback);

        hubConnection.start();
        hubConnection.send("inc");

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(1, value.get());
    }

    @Test
    public void SendWithParamTriggersOnHandler() throws InterruptedException {
        AtomicReference<String> value = new AtomicReference<>();
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        Action callback = (param) -> {
            assertNull(value.get());
            value.set(((JsonArray) param).get(0).getAsString());
        };
        hubConnection.On("inc", callback);

        hubConnection.start();
        hubConnection.send("inc", "Hello World");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value.get());
    }

    private class MockEchoTransport implements Transport {
        private OnReceiveCallBack onReceiveCallBack;

        @Override
        public void start() {}

        @Override
        public void send(String message) {
            this.onReceive(message);
        }

        @Override
        public void setOnReceive(OnReceiveCallBack callback) {
            this.onReceiveCallBack = callback;
        }

        @Override
        public void onReceive(String message) {
            this.onReceiveCallBack.invoke(message);
        }

        @Override
        public void stop() {return;}
    }
}