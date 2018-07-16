// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import org.junit.Test;

import java.util.concurrent.atomic.AtomicReference;

import static org.junit.Assert.*;

public class HubConnectionTest {
    @Test
    public void checkHubConnectionState() throws InterruptedException {
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);
        hubConnection.start();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void RegisteringMultipleHandlersAndBothGetTriggered() throws Exception {

        AtomicReference<Double> value = new AtomicReference<>(0.0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);
        hubConnection.on("inc", action);

        assertEquals(0.0, value.get(), 0);

        hubConnection.start();
        hubConnection.send("inc");

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(2, value.get(), 0);
    }

    @Test
    public void RemoveHandlerByName() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);

        assertEquals(0.0, value.get(), 0);

        hubConnection.start();
        hubConnection.send("inc");

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(1, value.get(), 0);

        hubConnection.remove("inc");
        hubConnection.send("inc");
        assertEquals(1, value.get(), 0);
    }

    @Test
    public void AddAndRemoveHandlerImmediately() throws Exception {

        AtomicReference<Double> value = new AtomicReference<>(0.0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);
        hubConnection.remove("inc");

        assertEquals(0.0, value.get(), 0);

        hubConnection.start();
        hubConnection.send("inc");

        // Confirming that the handler was removed.
        assertEquals(0, value.get(), 0);
    }

    @Test
    public void RemovingMultipleHandlersWithOneCallToRemove() throws Exception {

        AtomicReference<Double> value = new AtomicReference<>(0.0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> value.getAndUpdate((val) -> val + 2);

        hubConnection.on("inc", action);
        hubConnection.on("inc", secondAction);

        assertEquals(0.0, value.get(), 0);

        hubConnection.start();
        hubConnection.send("inc");

        assertEquals(3, value.get(), 0);

        hubConnection.remove("inc");
        hubConnection.send("inc");

        // Confirm that another invocation doesn't change anything because the handlers have been removed.
        assertEquals(3, value.get(), 0);
    }

    @Test
    public void RegisteringMultipleHandlersThatTakeParamsAndBothGetTriggered() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        Action1<Double> action = (number) -> value.getAndUpdate((val) -> val + number);

        hubConnection.on("add", action, Double.class);
        hubConnection.on("add", action, Double.class);

        assertEquals(0, value.get(), 0);
        hubConnection.start();
        hubConnection.send("add", 12);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals(24, value.get(), 0);
    }

    // We're using AtomicReference<Double> in the send tests instead of int here because Gson has trouble deserializing to Integer
    @Test
    public void SendWithNoParamsTriggersOnHandler() throws Exception {

        AtomicReference<Double> value = new AtomicReference<Double>(0.0);
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", () ->{
            assertEquals(0.0, value.get(), 0);
            value.getAndUpdate((val) -> val + 1);
        });

        hubConnection.start();
        hubConnection.send("inc");

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(1, value.get(), 0);
    }

    @Test
    public void SendWithParamTriggersOnHandler() throws Exception {
        AtomicReference<String> value = new AtomicReference<>();
        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param) ->{
            assertNull(value.get());
            value.set(param);
        }, String.class);

        hubConnection.start();
        hubConnection.send("inc", "Hello World");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value.get());
    }

    @Test
    public void SendWithTwoParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<Double> value2 = new AtomicReference<>();

        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2) ->{
            assertNull(value1.get());
            assertNull((value2.get()));

            value1.set(param1);
            value2.set(param2);
        }, String.class, Double.class);

        hubConnection.start();
        hubConnection.send("inc", "Hello World", 12);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value1.get());
        assertEquals(12, value2.get(), 0);
    }

    @Test
    public void SendWithThreeParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();

        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3) ->{
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
        }, String.class, String.class, String.class);

        hubConnection.start();
        hubConnection.send("inc", "A", "B", "C");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
    }

    @Test
    public void SendWithFourParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<String> value4 = new AtomicReference<>();

        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3, param4) ->{
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());
            assertNull(value4.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
        }, String.class, String.class, String.class, String.class);

        hubConnection.start();
        hubConnection.send("inc", "A", "B", "C", "D");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertEquals("D", value4.get());
    }

    @Test
    public void SendWithFiveParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();

        Transport mockTransport = new MockEchoTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3, param4, param5) ->{
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());
            assertNull(value4.get());
            assertNull(value5.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
        }, String.class, String.class, String.class, Boolean.class, Double.class);

        hubConnection.start();
        hubConnection.send("inc", "A", "B", "C", true, 12.0);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(12, value5.get(), 0);
    }

    private class MockEchoTransport implements Transport {
        private OnReceiveCallBack onReceiveCallBack;

        @Override
        public void start() {}

        @Override
        public void send(String message) throws Exception {
            this.onReceive(message);
        }

        @Override
        public void setOnReceive(OnReceiveCallBack callback) {
            this.onReceiveCallBack = callback;
        }

        @Override
        public void onReceive(String message) throws Exception {
            this.onReceiveCallBack.invoke(message);
        }

        @Override
        public void stop() {return;}
    }
}