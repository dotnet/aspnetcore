// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.List;
import java.util.concurrent.CancellationException;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Test;


class HubConnectionTest {
    private static final String RECORD_SEPARATOR = "\u001e";

    @Test
    public void checkHubConnectionState() throws Exception {
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        hubConnection.start();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void constructHubConnectionWithHttpConnectionOptions() throws Exception {
        Transport mockTransport = new MockTransport();
        HttpConnectionOptions options = new HttpConnectionOptions();
        options.setTransport(mockTransport);
        options.setLoglevel(LogLevel.Information);
        options.setSkipNegotiate(true);
        HubConnection hubConnection = new HubConnectionBuilder().withUrl("http://www.example.com", options).build();
        hubConnection.start();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionClosesAfterCloseMessage() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        mockTransport.receiveMessage("{\"type\":7,\"error\": \"There was an error\"}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionReceiveHandshakeResponseWithError() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        Throwable exception = assertThrows(HubException.class, () -> mockTransport.receiveMessage("{\"error\":\"Requested protocol 'messagepack' is not available.\"}" + RECORD_SEPARATOR));
        assertEquals("Error in handshake Requested protocol 'messagepack' is not available.", exception.getMessage());
    }

    @Test
    public void registeringMultipleHandlersAndBothGetTriggered() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);
        hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();

        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(2), value.get());
    }

    @Test
    public void removeHandlerByName() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(1), value.get());

        hubConnection.remove("inc");
        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void addAndRemoveHandlerImmediately() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);
        hubConnection.remove("inc");

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that the handler was removed.
        assertEquals(Double.valueOf(0), value.get());
    }

    @Test
    public void removingMultipleHandlersWithOneCallToRemove() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> value.getAndUpdate((val) -> val + 2);

        hubConnection.on("inc", action);
        hubConnection.on("inc", secondAction);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        assertEquals(Double.valueOf(3), value.get());

        hubConnection.remove("inc");

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirm that another invocation doesn't change anything because the handlers have been removed.
        assertEquals(Double.valueOf(3), value.get());
    }

    @Test
    public void removeHandlerWithUnsubscribe() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        Subscription subscription = hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(1), value.get());

        subscription.unsubscribe();
        try {
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        } catch (Exception ex) {
            assertEquals("There are no callbacks registered for the method 'inc'.", ex.getMessage());
        }

        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void unsubscribeTwice() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(),true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        Subscription subscription = hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(1), value.get());

        subscription.unsubscribe();
        subscription.unsubscribe();
        try {
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        } catch (Exception ex) {
            assertEquals("There are no callbacks registered for the method 'inc'.", ex.getMessage());
        }

        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void removeSingleHandlerWithUnsubscribe() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> value.getAndUpdate((val) -> val + 2);

        Subscription subscription = hubConnection.on("inc", action);
        Subscription secondSubscription = hubConnection.on("inc", secondAction);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(3), value.get());

        // This removes the first handler so when "inc" is invoked secondAction should still run.
        subscription.unsubscribe();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        assertEquals(Double.valueOf(5), value.get());
    }

    @Test
    public void addAndRemoveHandlerImmediatelyWithSubscribe() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        Subscription sub = hubConnection.on("inc", action);
        sub.unsubscribe();

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        try {
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        } catch (Exception ex) {
            assertEquals("There are no callbacks registered for the method 'inc'.", ex.getMessage());
        }

        // Confirming that the handler was removed.
        assertEquals(Double.valueOf(0), value.get());
    }

    @Test
    public void registeringMultipleHandlersThatTakeParamsAndBothGetTriggered() throws Exception {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        Action1<Double> action = (number) -> value.getAndUpdate((val) -> val + number);

        hubConnection.on("add", action, Double.class);
        hubConnection.on("add", action, Double.class);

        assertEquals(Double.valueOf(0), value.get());
        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"add\",\"arguments\":[12]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals(Double.valueOf(24), value.get());
    }

    @Test
    public void invokeWaitsForCompletionMessage() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        CompletableFuture<Integer> result = hubConnection.invoke(Integer.class, "echo", "message");
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(result.isDone());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertEquals(Integer.valueOf(42), result.get(1000L, TimeUnit.MILLISECONDS));
    }

    @Test
    public void multipleInvokesWaitForOwnCompletionMessage() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        CompletableFuture<Integer> result = hubConnection.invoke(Integer.class, "echo", "message");
        CompletableFuture<String> result2 = hubConnection.invoke(String.class, "echo", "message");
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertEquals("{\"type\":1,\"invocationId\":\"2\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[2]);
        assertFalse(result.isDone());
        assertFalse(result2.isDone());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"2\",\"result\":\"message\"}" + RECORD_SEPARATOR);
        assertEquals("message", result2.get(1000L, TimeUnit.MILLISECONDS));
        assertFalse(result.isDone());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);
        assertEquals(Integer.valueOf(42), result.get(1000L, TimeUnit.MILLISECONDS));
    }

    @Test
    public void invokeWorksForPrimitiveTypes() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        // int.class is a primitive type and since we use Class.cast to cast an Object to the expected return type
        // which does not work for primitives we have to write special logic for that case.
        CompletableFuture<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        assertFalse(result.isDone());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertEquals(Integer.valueOf(42), result.get(1000L, TimeUnit.MILLISECONDS));
    }

    @Test
    public void completionMessageCanHaveError() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        CompletableFuture<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        assertFalse(result.isDone());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);

        String exceptionMessage = null;
        try {
            result.get(1000L, TimeUnit.MILLISECONDS);
            assertFalse(true);
        } catch (Exception ex) {
            exceptionMessage = ex.getMessage();
        }

        assertEquals("com.microsoft.aspnet.signalr.HubException: There was an error", exceptionMessage);
    }

    @Test
    public void stopCancelsActiveInvokes() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        CompletableFuture<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        assertFalse(result.isDone());

        hubConnection.stop();

        boolean hasException = false;
        try {
            result.get(1000L, TimeUnit.MILLISECONDS);
            assertFalse(true);
        } catch (CancellationException ex) {
            hasException = true;
        }

        assertTrue(hasException);
    }

    @Test
    public void sendWithNoParamsTriggersOnHandler() throws Exception {
        AtomicReference<Integer> value = new AtomicReference<>(0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", () ->{
            assertEquals(Integer.valueOf(0), value.get());
            value.getAndUpdate((val) -> val + 1);
        });

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Integer.valueOf(1), value.get());
    }

    @Test
    public void sendWithParamTriggersOnHandler() throws Exception {
        AtomicReference<String> value = new AtomicReference<>();
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param) ->{
            assertNull(value.get());
            value.set(param);
        }, String.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\"]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "Hello World");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value.get());
    }

    @Test
    public void sendWithTwoParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<Double> value2 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param1, param2) ->{
            assertNull(value1.get());
            assertNull((value2.get()));

            value1.set(param1);
            value2.set(param2);
        }, String.class, Double.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\", 12]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "Hello World", 12);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value1.get());
        assertEquals(Double.valueOf(12), value2.get());
    }

    @Test
    public void sendWithThreeParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param1, param2, param3) ->{
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
        }, String.class, String.class, String.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\"]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "A", "B", "C");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
    }

    @Test
    public void sendWithFourParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<String> value4 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

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
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\", \"D\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertEquals("D", value4.get());
    }

    @Test
    public void sendWithFiveParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

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
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12 ]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
    }

    @Test
    public void sendWithSixParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param1, param2, param3, param4, param5, param6) -> {
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());
            assertNull(value4.get());
            assertNull(value5.get());
            assertNull(value6.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
        }, String.class, String.class, String.class, Boolean.class, Double.class, String.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12,\"D\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
        assertEquals("D", value6.get());
    }

    @Test
    public void sendWithSevenParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();
        AtomicReference<String> value7 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param1, param2, param3, param4, param5, param6, param7) -> {
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());
            assertNull(value4.get());
            assertNull(value5.get());
            assertNull(value6.get());
            assertNull(value7.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
            value7.set(param7);
        }, String.class, String.class, String.class, Boolean.class, Double.class, String.class, String.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12,\"D\",\"E\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
        assertEquals("D", value6.get());
        assertEquals("E", value7.get());
    }

    @Test
    public void sendWithEightParamsTriggersOnHandler() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();
        AtomicReference<String> value7 = new AtomicReference<>();
        AtomicReference<String> value8 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param1, param2, param3, param4, param5, param6, param7, param8) -> {
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());
            assertNull(value4.get());
            assertNull(value5.get());
            assertNull(value6.get());
            assertNull(value7.get());
            assertNull(value8.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
            value7.set(param7);
            value8.set(param8);
        }, String.class, String.class, String.class, Boolean.class, Double.class, String.class, String.class, String.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12,\"D\",\"E\",\"F\"]}" + RECORD_SEPARATOR);
        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
        assertEquals("D", value6.get());
        assertEquals("E", value7.get());
        assertEquals("F", value8.get());
    }

    private class Custom {
        public int number;
        public String str;
        public boolean[] bools;
    }

    @Test
    public void sendWithCustomObjectTriggersOnHandler() throws Exception {
        AtomicReference<Custom> value1 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", (param1) -> {
            assertNull(value1.get());

            value1.set(param1);
        }, Custom.class);

        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[{\"number\":1,\"str\":\"A\",\"bools\":[true,false]}]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        Custom custom = value1.get();
        assertEquals(1, custom.number);
        assertEquals("A", custom.str);
        assertEquals(2, custom.bools.length);
        assertEquals(true, custom.bools[0]);
        assertEquals(false, custom.bools[1]);
    }

    @Test
    public void receiveHandshakeResponseAndMessage() throws Exception {
        AtomicReference<Double> value = new AtomicReference<Double>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());

        hubConnection.on("inc", () ->{
            assertEquals(Double.valueOf(0), value.get());
            value.getAndUpdate((val) -> val + 1);
        });

        // On start we're going to receive the handshake response and also an invocation in the same payload.
        hubConnection.start();
        String expectedSentMessage  = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;
        assertEquals(expectedSentMessage, mockTransport.getSentMessages()[0]);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR + "{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void onClosedCallbackRunsWhenStopIsCalled() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        hubConnection.start();
        hubConnection.onClosed((ex) -> {
            assertNull(value1.get());
            value1.set("Closed callback ran.");
        });
        hubConnection.stop();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertEquals(value1.get(), "Closed callback ran.");
    }

    @Test
    public void multipleOnClosedCallbacksRunWhenStopIsCalled() throws Exception {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        hubConnection.start();

        hubConnection.onClosed((ex) -> {
            assertNull(value1.get());
            value1.set("Closed callback ran.");
        });

        hubConnection.onClosed((ex) -> {
            assertNull(value2.get());
            value2.set("The second onClosed callback ran");
        });

        assertNull(value1.get());
        assertNull(value2.get());
        hubConnection.stop();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertEquals("Closed callback ran.",value1.get());
        assertEquals("The second onClosed callback ran", value2.get());
    }

    @Test
    public void hubConnectionClosesAndRunsOnClosedCallbackAfterCloseMessageWithError() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        hubConnection.onClosed((ex) -> {
            assertEquals(ex.getMessage(), "There was an error");
        });
        hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        mockTransport.receiveMessage("{\"type\":7,\"error\": \"There was an error\"}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void callingStartOnStartedHubConnectionNoOps() throws Exception {
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        hubConnection.start();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.start();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void cannotSendBeforeStart() throws Exception {
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        Throwable exception = assertThrows(HubException.class, () -> hubConnection.send("inc"));
        assertEquals("The 'send' method cannot be called if the connection is not active", exception.getMessage());
    }

    @Test
    public void errorWhenReceivingInvokeWithIncorrectArgumentLength() throws Exception {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = new HubConnection("http://example.com", mockTransport, new NullLogger(), true, new TestHttpClient());
        hubConnection.on("Send", (s) -> {
            assertTrue(false);
        }, String.class);

        CompletableFuture<Void> startFuture = hubConnection.start();
        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR);

        startFuture.get(1000, TimeUnit.MILLISECONDS);
        RuntimeException exception = assertThrows(RuntimeException.class, () -> mockTransport.receiveMessage("{\"type\":1,\"target\":\"Send\",\"arguments\":[]}" + RECORD_SEPARATOR));
        assertEquals("Invocation provides 0 argument(s) but target expects 1.", exception.getMessage());
    }

    @Test
    public void negotiateSentOnStart() {
        TestHttpClient client = new TestHttpClient()
        .on("POST", (req) -> CompletableFuture.completedFuture(new HttpResponse(404, "", "")));

        HubConnection hubConnection = new HubConnectionBuilder()
            .withUrl("http://example.com")
            .configureHttpClient(client)
            .build();

        try {
            hubConnection.start().get(1000, TimeUnit.MILLISECONDS);
        } catch(Exception ex) {}

        List<HttpRequest> sentRequests = client.getSentRequests();
        assertEquals(1, sentRequests.size());
        assertEquals("http://example.com/negotiate", sentRequests.get(0).getUrl());
    }

    @Test
    public void negotiateThatRedirectsForeverFailsAfter100Tries() throws InterruptedException, TimeoutException, Exception {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate",
                (req) -> CompletableFuture.completedFuture(new HttpResponse(200, "", "{\"url\":\"http://example.com\"}")));

        HubConnection hubConnection = new HubConnectionBuilder().withUrl("http://example.com")
                .configureHttpClient(client).build();

        ExecutionException exception = assertThrows(ExecutionException.class, () -> hubConnection.start().get(1000, TimeUnit.MILLISECONDS));
        assertEquals("Negotiate redirection limit exceeded.", exception.getCause().getMessage());
    }

    @Test
    public void afterSuccessfulNegotiateConnectsWithTransport() throws InterruptedException, TimeoutException, Exception {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate",
                (req) -> CompletableFuture.completedFuture(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport();
        HttpConnectionOptions options = new HttpConnectionOptions();
        options.setTransport(transport);
        HubConnection hubConnection = new HubConnectionBuilder().withUrl("http://example.com", options)
                .configureHttpClient(client).build();

        hubConnection.start().get(1000, TimeUnit.MILLISECONDS);

        String[] sentMessages = transport.getSentMessages();
        assertEquals(1, sentMessages.length);
        assertEquals("{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR, sentMessages[0]);
    }

    @Test
    public void negotiateThatReturnsErrorThrowsFromStart() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate",
                (req) -> CompletableFuture.completedFuture(new HttpResponse(200, "", "{\"error\":\"Test error.\"}")));

        MockTransport transport = new MockTransport();
        HttpConnectionOptions options = new HttpConnectionOptions();
        options.setTransport(transport);
        HubConnection hubConnection = new HubConnectionBuilder().withUrl("http://example.com", options)
                .configureHttpClient(client).build();

        ExecutionException exception = assertThrows(ExecutionException.class, () -> hubConnection.start().get(1000, TimeUnit.MILLISECONDS));
        assertEquals("Test error.", exception.getCause().getMessage());
    }

    @Test
    public void negotiateRedirectIsFollowed()
            throws InterruptedException, ExecutionException, TimeoutException, Exception {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate",
                (req) -> CompletableFuture.completedFuture(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\"}")))
                .on("POST", "http://testexample.com/negotiate",
                (req) -> CompletableFuture.completedFuture(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport();
        HttpConnectionOptions options = new HttpConnectionOptions();
        options.setTransport(transport);
        HubConnection hubConnection = new HubConnectionBuilder().withUrl("http://example.com", options)
                .configureHttpClient(client).build();

        hubConnection.start().get(1000, TimeUnit.MILLISECONDS);
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
    }
}