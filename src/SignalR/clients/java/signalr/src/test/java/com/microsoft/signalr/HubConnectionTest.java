// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.Iterator;
import java.util.List;
import java.util.concurrent.CancellationException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Test;

import io.reactivex.Completable;
import io.reactivex.Observable;
import io.reactivex.Single;
import io.reactivex.disposables.Disposable;
import io.reactivex.subjects.CompletableSubject;
import io.reactivex.subjects.PublishSubject;
import io.reactivex.subjects.ReplaySubject;
import io.reactivex.subjects.SingleSubject;

class HubConnectionTest {
    private static final String RECORD_SEPARATOR = "\u001e";

    @Test
    public void checkHubConnectionState() {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void transportCloseTriggersStopInHubConnection() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        mockTransport.stop();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void transportCloseWithErrorTriggersStopInHubConnection() {
        MockTransport mockTransport = new MockTransport();
        AtomicReference<String> message = new AtomicReference<>();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        String errorMessage = "Example transport error.";

        hubConnection.onClosed((error) -> {
            message.set(error.getMessage());
        });

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        mockTransport.stopWithError(errorMessage);
        assertEquals(errorMessage, message.get());
    }

    @Test
    public void checkHubConnectionStateNoHandShakeResponse() {
        MockTransport mockTransport = new MockTransport(false);
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(mockTransport)
                .withHttpClient(new TestHttpClient())
                .shouldSkipNegotiate(true)
                .withHandshakeResponseTimeout(100)
                .build();
        Throwable exception = assertThrows(RuntimeException.class, () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals(TimeoutException.class, exception.getCause().getClass());
        assertEquals("Timed out waiting for the server to respond to the handshake message.", exception.getCause().getMessage());
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void constructHubConnectionWithHttpConnectionOptions() {
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionClosesAfterCloseMessage() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        mockTransport.receiveMessage("{\"type\":7,\"error\": \"There was an error\"}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionUrlCanBeChanged() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        hubConnection.setBaseUrl("http://newurl.com");
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals("http://newurl.com", hubConnection.getBaseUrl());

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void canUpdateUrlInOnClosed() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        hubConnection.onClosed((error) -> {
            hubConnection.setBaseUrl("http://newurl.com");
        });

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals("http://newurl.com", hubConnection.getBaseUrl());

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void changingUrlWhenConnectedThrows() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        Throwable exception = assertThrows(IllegalStateException.class, () -> hubConnection.setBaseUrl("http://newurl.com"));
        assertEquals("The HubConnection must be in the disconnected state to change the url.",exception.getMessage());
    }

    @Test
    public void settingNewUrlToNullThrows() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        Throwable exception = assertThrows(IllegalArgumentException.class, () -> hubConnection.setBaseUrl(null));
        assertEquals("The HubConnection url must be a valid url.",exception.getMessage());
    }

    @Test
    public void invalidHandShakeResponse() {
        MockTransport mockTransport = new MockTransport(false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start();
        mockTransport.getStartTask().timeout(1, TimeUnit.SECONDS).blockingAwait();

        Throwable exception = assertThrows(RuntimeException.class, () -> mockTransport.receiveMessage("{" + RECORD_SEPARATOR));
        assertEquals("An invalid handshake response was received from the server.", exception.getMessage());
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionReceiveHandshakeResponseWithError() {
        MockTransport mockTransport = new MockTransport(false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start();
        mockTransport.getStartTask().timeout(1, TimeUnit.SECONDS).blockingAwait();
        Throwable exception = assertThrows(RuntimeException.class, () -> mockTransport.receiveMessage("{\"error\":\"Requested protocol 'messagepack' is not available.\"}" + RECORD_SEPARATOR));
        assertEquals("Error in handshake Requested protocol 'messagepack' is not available.", exception.getMessage());
    }

    @Test
    public void registeringMultipleHandlersAndBothGetTriggered() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);
        hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(2), value.get());
    }

    @Test
    public void removeHandlerByName() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(1), value.get());

        hubConnection.remove("inc");
        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void addAndRemoveHandlerImmediately() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        hubConnection.on("inc", action);
        hubConnection.remove("inc");

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that the handler was removed.
        assertEquals(Double.valueOf(0), value.get());
    }

    @Test
    public void removingMultipleHandlersWithOneCallToRemove() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> value.getAndUpdate((val) -> val + 2);

        hubConnection.on("inc", action);
        hubConnection.on("inc", secondAction);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        assertEquals(Double.valueOf(3), value.get());

        hubConnection.remove("inc");

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirm that another invocation doesn't change anything because the handlers have been removed.
        assertEquals(Double.valueOf(3), value.get());
    }

    @Test
    public void removeHandlerWithUnsubscribe() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        Subscription subscription = hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

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
    public void unsubscribeTwice() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        Subscription subscription = hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

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
    public void removeSingleHandlerWithUnsubscribe() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> value.getAndUpdate((val) -> val + 2);

        Subscription subscription = hubConnection.on("inc", action);
        Subscription secondSubscription = hubConnection.on("inc", secondAction);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String message = mockTransport.getSentMessages()[0];
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(3), value.get());

        // This removes the first handler so when "inc" is invoked secondAction should still run.
        subscription.unsubscribe();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        assertEquals(Double.valueOf(5), value.get());
    }

    @Test
    public void addAndRemoveHandlerImmediatelyWithSubscribe() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);

        Subscription sub = hubConnection.on("inc", action);
        sub.unsubscribe();

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        try {
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        } catch (Exception ex) {
            assertEquals("There are no callbacks registered for the method 'inc'.", ex.getMessage());
        }

        // Confirming that the handler was removed.
        assertEquals(Double.valueOf(0), value.get());
    }

    @Test
    public void registeringMultipleHandlersThatTakeParamsAndBothGetTriggered() {
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        Action1<Double> action = (number) -> value.getAndUpdate((val) -> val + number);

        hubConnection.on("add", action, Double.class);
        hubConnection.on("add", action, Double.class);

        assertEquals(Double.valueOf(0), value.get());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"add\",\"arguments\":[12]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals(Double.valueOf(24), value.get());
    }

    @Test
    public void checkStreamUploadSingleItemThroughSend() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);

        stream.onNext("FirstItem");
        String[] messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[2]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[3]);
    }

    @Test
    public void checkStreamUploadMultipleStreamsThroughSend() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> firstStream = ReplaySubject.create();
        ReplaySubject<String> secondStream = ReplaySubject.create();

        hubConnection.send("UploadStream", firstStream, secondStream);

        firstStream.onNext("First Stream 1");
        secondStream.onNext("Second Stream 1");
        String[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First Stream 1\"}\u001E", messages[2]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"Second Stream 1\"}\u001E", messages[3]);

        firstStream.onComplete();
        secondStream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(6, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[4]);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", messages[5]);
    }

    @Test
    public void checkStreamUploadThroughSendWithArgs() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream, 12);

        stream.onNext("FirstItem");
        String[] messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[12],\"streamIds\":[\"1\"]}\u001E", messages[1]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[2]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[3]);
    }

    @Test
    public void streamMapIsClearedOnClose() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream, 12);

        stream.onNext("FirstItem");
        String[] messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[12],\"streamIds\":[\"1\"]}\u001E", messages[1]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[2]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[3]);

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertTrue(hubConnection.getStreamMap().isEmpty());
    }

    @Test
    public void streamMapEntriesRemovedOnStreamClose() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream, 12);

        ReplaySubject<String> secondStream = ReplaySubject.create();
        hubConnection.send("SecondUploadStream", secondStream, 13);


        stream.onNext("FirstItem");
        secondStream.onNext("SecondItem");
        String[] messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[12],\"streamIds\":[\"1\"]}\u001E", messages[1]);
        assertEquals("{\"type\":1,\"target\":\"SecondUploadStream\",\"arguments\":[13],\"streamIds\":[\"2\"]}\u001E", messages[2]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[3]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"SecondItem\"}\u001E", messages[4]);


        assertEquals(2, hubConnection.getStreamMap().size());
        assertTrue(hubConnection.getStreamMap().keySet().contains("1"));
        assertTrue(hubConnection.getStreamMap().keySet().contains("2"));

        // Verify that we clear the entry from the stream map after we clear the first stream.
        stream.onComplete();
        assertEquals(1, hubConnection.getStreamMap().size());
        assertTrue(hubConnection.getStreamMap().keySet().contains("2"));

        secondStream.onError(new Exception("Exception"));
        assertEquals(0, hubConnection.getStreamMap().size());
        assertTrue(hubConnection.getStreamMap().isEmpty());

        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[5]);
        assertEquals("{\"type\":3,\"invocationId\":\"2\",\"error\":\"java.lang.Exception: Exception\"}\u001E", messages[6]);

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertTrue(hubConnection.getStreamMap().isEmpty());
    }

    @Test
    public void useSameSubjectMultipleTimes() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream, stream);

        stream.onNext("FirstItem");
        String[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"1\",\"2\"]}\u001E", messages[1]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[2]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", messages[3]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(6, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[4]);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", messages[5]);
    }

    @Test
    public void checkStreamUploadSingleItemThroughInvoke() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.invoke(String.class, "UploadStream", stream);

        stream.onNext("FirstItem");
        String[] messages = mockTransport.getSentMessages();
        assertEquals(3, messages.length);
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"2\"]}\u001E", messages[1]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", messages[2]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", messages[3]);
    }

    @Test
    public void checkStreamUploadSingleItemThroughStream() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.stream(String.class, "UploadStream", stream);

        stream.onNext("FirstItem");

        String[] messages = mockTransport.getSentMessages();
        assertEquals(3, messages.length);
        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"2\"]}\u001E", messages[1]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", messages[2]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", messages[3]);
    }

    @Test
    public void useSameSubjectInMutlipleStreamsFromDifferentMethods() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);
        hubConnection.invoke(String.class, "UploadStream", stream);
        hubConnection.stream(String.class, "UploadStream", stream);

        String[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"1\"]}\u001E", messages[1]);
        assertEquals("{\"type\":1,\"invocationId\":\"2\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"3\"]}\u001E", messages[2]);
        assertEquals("{\"type\":4,\"invocationId\":\"4\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"5\"]}\u001E", messages[3]);

        stream.onNext("FirstItem");

        messages = mockTransport.getSentMessages();
        assertEquals(7, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[4]);
        assertEquals("{\"type\":2,\"invocationId\":\"3\",\"item\":\"FirstItem\"}\u001E", messages[5]);
        assertEquals("{\"type\":2,\"invocationId\":\"5\",\"item\":\"FirstItem\"}\u001E", messages[6]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(10, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[7]);
        assertEquals("{\"type\":3,\"invocationId\":\"3\"}\u001E", messages[8]);
        assertEquals("{\"type\":3,\"invocationId\":\"5\"}\u001E", messages[9]);
    }

    @Test
    public void streamUploadCallOnError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onError(new RuntimeException("onError called"));
        String[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[2]);
        assertEquals("{\"type\":3,\"invocationId\":\"1\",\"error\":\"java.lang.RuntimeException: onError called\"}\u001E", messages[3]);

        // onComplete doesn't send a completion message after onError.
        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
    }

    @Test
    public void checkStreamUploadMultipleItemsThroughSend() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onNext("SecondItem");
        stream.onNext("ThirdItem");

        String[] messages = mockTransport.getSentMessages();
        assertEquals(5, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", messages[2]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"SecondItem\"}\u001E", messages[3]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"ThirdItem\"}\u001E", messages[4]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(6, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[5]);
    }

    @Test
    public void checkStreamUploadMultipleItemsThroughInvoke() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.invoke(String.class, "UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onNext("SecondItem");

        String[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", messages[2]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"SecondItem\"}\u001E", messages[3]);

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(5, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", messages[4]);
    }

    @Test
    public void canStartAndStopMultipleStreams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        PublishSubject<String> streamOne = PublishSubject.create();
        PublishSubject<String> streamTwo = PublishSubject.create();

        hubConnection.send("UploadStream", streamOne);
        hubConnection.send("UploadStream", streamTwo);

        streamOne.onNext("Stream One First Item");
        streamTwo.onNext("Stream Two First Item");
        streamOne.onNext("Stream One Second Item");
        streamTwo.onNext("Stream Two Second Item");

        streamOne.onComplete();
        streamTwo.onComplete();
        String[] messages = mockTransport.getSentMessages();

        // Handshake message + 2 calls to send + 4 calls to onNext + 2 calls to onComplete = 9
        assertEquals(9, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Stream One First Item\"}\u001E", messages[3]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"Stream Two First Item\"}\u001E", messages[4]);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Stream One Second Item\"}\u001E", messages[5]);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"Stream Two Second Item\"}\u001E", messages[6]);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", messages[7]);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", messages[8]);
    }

    @Test
    public void checkStreamSingleItem() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> {},
                () -> completed.set(true));

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(completed.get());
        assertFalse(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        assertTrue(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"hello\"}" + RECORD_SEPARATOR);
        assertTrue(completed.get());

        assertEquals("First", result.timeout(1000, TimeUnit.MILLISECONDS).blockingFirst());
    }

    @Test
    public void checkStreamCompletionResult() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> {},
                () -> completed.set(true));

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(completed.get());
        assertFalse(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        assertTrue(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"COMPLETED\"}" + RECORD_SEPARATOR);
        assertTrue(completed.get());

        assertEquals("First", result.timeout(1000, TimeUnit.MILLISECONDS).blockingFirst());
        assertEquals("COMPLETED", result.timeout(1000, TimeUnit.MILLISECONDS).blockingLast());
    }

    @Test
    public void checkStreamCompletionError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean onErrorCalled = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> onErrorCalled.set(true),
                () -> {});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(onErrorCalled.get());
        assertFalse(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        assertTrue(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);
        assertTrue(onErrorCalled.get());

        assertEquals("First", result.timeout(1000, TimeUnit.MILLISECONDS).blockingFirst());
        Throwable exception = assertThrows(HubException.class, () -> result.timeout(1000, TimeUnit.MILLISECONDS).blockingLast());
        assertEquals("There was an error", exception.getMessage());
    }

    @Test
    public void checkStreamMultipleItems() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(completed.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Second\"}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"null\"}" + RECORD_SEPARATOR);

        Iterator<String> resultIterator = result.timeout(1000, TimeUnit.MILLISECONDS).blockingIterable().iterator();
        assertEquals("First", resultIterator.next());
        assertEquals("Second", resultIterator.next());
        assertTrue(completed.get());
    }

    @Test
    public void checkCancelIsSentAfterDispose() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(completed.get());

        subscription.dispose();
        assertEquals("{\"type\":5,\"invocationId\":\"1\"}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[2]);
    }

    @Test
    public void checkCancelIsSentAfterAllSubscriptionsAreDisposed() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        Disposable secondSubscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        subscription.dispose();
        assertEquals(2, mockTransport.getSentMessages().length);
        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                mockTransport.getSentMessages()[mockTransport.getSentMessages().length - 1]);

        secondSubscription.dispose();
        assertEquals(3, mockTransport.getSentMessages().length);
        assertEquals("{\"type\":5,\"invocationId\":\"1\"}" + RECORD_SEPARATOR,
                mockTransport.getSentMessages()[mockTransport.getSentMessages().length - 1]);
    }

    @Test
    public void checkStreamWithDispose() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        subscription.dispose();
        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Second\"}" + RECORD_SEPARATOR);

        assertEquals("First", result.timeout(1000, TimeUnit.MILLISECONDS).blockingLast());
    }

    @Test
    public void checkStreamWithDisposeWithMultipleSubscriptions() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        Disposable subscription2 = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(completed.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        subscription.dispose();
        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Second\"}" + RECORD_SEPARATOR);

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\"}" + RECORD_SEPARATOR);
        assertTrue(completed.get());
        assertEquals("First", result.timeout(1000, TimeUnit.MILLISECONDS).blockingFirst());

        subscription2.dispose();
        assertEquals("Second", result.timeout(1000, TimeUnit.MILLISECONDS).blockingLast());
    }

    @Test
    public void invokeWaitsForCompletionMessage() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(Integer.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertEquals(Integer.valueOf(42), result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void invokeNoReturnValueWaitsForCompletion() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe();

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\"}" + RECORD_SEPARATOR);

        assertNull(result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void invokeCompletedByCompletionMessageWithResult() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe();

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertNull(result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void completionWithResultAndErrorHandlesError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe(() -> {}, (error) -> {});

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        Throwable exception = assertThrows(IllegalArgumentException.class, () -> mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42,\"error\":\"There was an error\"}" + RECORD_SEPARATOR));
        assertEquals("Expected either 'error' or 'result' to be provided, but not both.", exception.getMessage());
    }

    @Test
    public void invokeNoReturnValueHandlesError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe(() -> {}, (error) -> {});

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);

        result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet();

        AtomicReference<String> errorMessage = new AtomicReference<>();
        result.doOnError(error -> {
            errorMessage.set(error.getMessage());
        }).subscribe(() -> {}, (error) -> {});

        assertEquals("There was an error", errorMessage.get());
    }

    @Test
    public void canSendNullArgInInvocation() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<String> result = hubConnection.invoke(String.class, "fixedMessage", (Object)null);
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"fixedMessage\",\"arguments\":[null]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"Hello World\"}" + RECORD_SEPARATOR);

        assertEquals("Hello World", result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void canSendMultipleNullArgsInInvocation() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<String> result = hubConnection.invoke(String.class, "fixedMessage", null, null);
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"fixedMessage\",\"arguments\":[null,null]}"+ RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"Hello World\"}" + RECORD_SEPARATOR);

        assertEquals("Hello World", result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void multipleInvokesWaitForOwnCompletionMessage() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean doneFirst = new AtomicBoolean();
        AtomicBoolean doneSecond = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(Integer.class, "echo", "message");
        Single<String> result2 = hubConnection.invoke(String.class, "echo", "message");
        result.doOnSuccess(value -> doneFirst.set(true)).subscribe();
        result2.doOnSuccess(value -> doneSecond.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[1]);
        assertEquals("{\"type\":1,\"invocationId\":\"2\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR, mockTransport.getSentMessages()[2]);
        assertFalse(doneFirst.get());
        assertFalse(doneSecond.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"2\",\"result\":\"message\"}" + RECORD_SEPARATOR);
        assertEquals("message", result2.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertFalse(doneFirst.get());
        assertTrue(doneSecond.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);
        assertEquals(Integer.valueOf(42), result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(doneFirst.get());
    }

    @Test
    public void invokeWorksForPrimitiveTypes() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        // int.class is a primitive type and since we use Class.cast to cast an Object to the expected return type
        // which does not work for primitives we have to write special logic for that case.
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertEquals(Integer.valueOf(42), result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void completionMessageCanHaveError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);

        String exceptionMessage = null;
        try {
            result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet();
            assertFalse(true);
        } catch (HubException ex) {
            exceptionMessage = ex.getMessage();
        }

        assertEquals("There was an error", exceptionMessage);
    }

    @Test
    public void stopCancelsActiveInvokes() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true));
        assertFalse(done.get());

        hubConnection.stop();

        RuntimeException hasException = null;
        try {
            result.timeout(1000, TimeUnit.MILLISECONDS).blockingGet();
            assertFalse(true);
        } catch (CancellationException ex) {
            hasException = ex;
        }

        assertEquals("Invocation was canceled.", hasException.getMessage());
    }

    @Test
    public void sendWithNoParamsTriggersOnHandler() {
        AtomicReference<Integer> value = new AtomicReference<>(0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", () ->{
            assertEquals(Integer.valueOf(0), value.get());
            value.getAndUpdate((val) -> val + 1);
        });

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Integer.valueOf(1), value.get());
    }

    @Test
    public void sendWithParamTriggersOnHandler() {
        AtomicReference<String> value = new AtomicReference<>();
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param) ->{
            assertNull(value.get());
            value.set(param);
        }, String.class);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\"]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "Hello World");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value.get());
    }

    @Test
    public void sendWithTwoParamsTriggersOnHandler() {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<Double> value2 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2) ->{
            assertNull(value1.get());
            assertNull((value2.get()));

            value1.set(param1);
            value2.set(param2);
        }, String.class, Double.class);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\", 12]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "Hello World", 12);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("Hello World", value1.get());
        assertEquals(Double.valueOf(12), value2.get());
    }

    @Test
    public void sendWithThreeParamsTriggersOnHandler() {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3) ->{
            assertNull(value1.get());
            assertNull(value2.get());
            assertNull(value3.get());

            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
        }, String.class, String.class, String.class);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\"]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "A", "B", "C");

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
    }

    @Test
    public void sendWithFourParamsTriggersOnHandler() {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<String> value4 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

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

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\", \"D\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertEquals("D", value4.get());
    }

    @Test
    public void sendWithFiveParamsTriggersOnHandler()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

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

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12 ]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
    }

    @Test
    public void sendWithSixParamsTriggersOnHandler()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

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

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
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
    public void sendWithSevenParamsTriggersOnHandler()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();
        AtomicReference<String> value7 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

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

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
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
    public void sendWithEightParamsTriggersOnHandler()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();
        AtomicReference<String> value7 = new AtomicReference<>();
        AtomicReference<String> value8 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

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

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
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
    public void sendWithCustomObjectTriggersOnHandler()  {
        AtomicReference<Custom> value1 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1) -> {
            assertNull(value1.get());

            value1.set(param1);
        }, Custom.class);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
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
    public void receiveHandshakeResponseAndMessage() {
        AtomicReference<Double> value = new AtomicReference<Double>(0.0);
        MockTransport mockTransport = new MockTransport(false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", () -> {
            assertEquals(Double.valueOf(0), value.get());
            value.getAndUpdate((val) -> val + 1);
        });

        // On start we're going to receive the handshake response and also an invocation in the same payload.
        hubConnection.start();
        mockTransport.getStartTask().timeout(1, TimeUnit.SECONDS).blockingAwait();
        String expectedSentMessage  = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;
        assertEquals(expectedSentMessage, mockTransport.getSentMessages()[0]);

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR + "{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void onClosedCallbackRunsWhenStopIsCalled()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        hubConnection.onClosed((ex) -> {
            assertNull(value1.get());
            value1.set("Closed callback ran.");
        });
        hubConnection.stop();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertEquals(value1.get(), "Closed callback ran.");
    }

    @Test
    public void multipleOnClosedCallbacksRunWhenStopIsCalled()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

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
    public void hubConnectionClosesAndRunsOnClosedCallbackAfterCloseMessageWithError()  {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        hubConnection.onClosed((ex) -> {
            assertEquals(ex.getMessage(), "There was an error");
        });
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        mockTransport.receiveMessage("{\"type\":7,\"error\": \"There was an error\"}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void callingStartOnStartedHubConnectionNoOps()  {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void cannotSendBeforeStart()  {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        Throwable exception = assertThrows(RuntimeException.class, () -> hubConnection.send("inc"));
        assertEquals("The 'send' method cannot be called if the connection is not active.", exception.getMessage());
    }

    @Test
    public void cannotInvokeBeforeStart()  {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        Throwable exception = assertThrows(RuntimeException.class, () -> hubConnection.invoke(String.class, "inc", "arg1"));
        assertEquals("The 'invoke' method cannot be called if the connection is not active.", exception.getMessage());
    }

    @Test
    public void cannotStreamBeforeStart()  {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        Throwable exception = assertThrows(RuntimeException.class, () -> hubConnection.stream(String.class, "inc", "arg1"));
        assertEquals("The 'stream' method cannot be called if the connection is not active.", exception.getMessage());
    }

    @Test
    public void doesNotErrorWhenReceivingInvokeWithIncorrectArgumentLength()  {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        hubConnection.on("Send", (s) -> {
            assertTrue(false);
        }, String.class);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"Send\",\"arguments\":[]}" + RECORD_SEPARATOR);
        hubConnection.stop();
    }

    @Test
    public void negotiateSentOnStart() {
        TestHttpClient client = new TestHttpClient()
        .on("POST", (req) -> Single.just(new HttpResponse(404, "", "")));

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withHttpClient(client)
                .build();

        Exception exception = assertThrows(RuntimeException.class, () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Unexpected status code returned from negotiate: 404 .", exception.getMessage());

        List<HttpRequest> sentRequests = client.getSentRequests();
        assertEquals(1, sentRequests.size());
        assertEquals("http://example.com/negotiate?negotiateVersion=1", sentRequests.get(0).getUrl());
    }

    @Test
    public void negotiateThatRedirectsForeverFailsAfter100Tries() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", "{\"url\":\"http://example.com\"}")));

        HubConnection hubConnection = HubConnectionBuilder
            .create("http://example.com")
            .withHttpClient(client)
            .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
            () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Negotiate redirection limit exceeded.", exception.getMessage());
    }

    @Test
    public void noConnectionIdWhenSkippingNegotiate() {
        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .shouldSkipNegotiate(true)
                .build();

        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void connectionIdIsAvailableAfterStart() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void connectionTokenAppearsInQSConnectionIdIsOnConnectionInstance() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                                "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                                "\"negotiateVersion\": 1," +
                                "\"connectionToken\":\"connection-token-value\"," +
                                "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());
        assertEquals("http://example.com?id=connection-token-value", transport.getUrl());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void connectionTokenIsIgnoredIfNegotiateVersionIsNotPresentInNegotiateResponse() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                        "\"connectionToken\":\"connection-token-value\"," +
                        "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());
        assertEquals("http://example.com?id=bVOiRPG8-6YiJ6d7ZcTOVQ", transport.getUrl());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void negotiateVersionIsNotAddedIfAlreadyPresent() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=42",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                        "\"connectionToken\":\"connection-token-value\"," +
                        "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com?negotiateVersion=42")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());
        assertEquals("http://example.com?negotiateVersion=42&id=bVOiRPG8-6YiJ6d7ZcTOVQ", transport.getUrl());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void afterSuccessfulNegotiateConnectsWithWebsocketsTransport() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        String[] sentMessages = transport.getSentMessages();
        assertEquals(1, sentMessages.length);
        assertEquals("{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR, sentMessages[0]);
    }

    @Test
    public void afterSuccessfulNegotiateConnectsWithLongPollingTransport() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        String[] sentMessages = transport.getSentMessages();
        assertEquals(1, sentMessages.length);
        assertEquals("{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR, sentMessages[0]);
    }

    @Test
    public void TransportAllUsesLongPollingWhenServerOnlySupportLongPolling() {
        AtomicInteger requestCount = new AtomicInteger(0);
        CompletableSubject close = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("POST",
                        (req) -> Single.just(new HttpResponse(200, "",
                                "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                        + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")))
                .on("GET", (req) -> {
                    if (requestCount.get() < 2) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", "{}" + RECORD_SEPARATOR));
                    }
                    assertTrue(close.blockingAwait(5, TimeUnit.SECONDS));
                    return Single.just(new HttpResponse(204, "", ""));
                });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.ALL)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertTrue(hubConnection.getTransport() instanceof LongPollingTransport);
        close.onComplete();
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void ClientThatSelectsWebsocketsThrowsWhenWebsocketsAreNotAvailable() {
        TestHttpClient client = new TestHttpClient().on("POST",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")))
                .on("GET", (req) -> {
                    return Single.just(new HttpResponse(204, "", ""));
                });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.WEBSOCKETS)
                .withHttpClient(client)
                .build();

        assertEquals(TransportEnum.WEBSOCKETS, hubConnection.getTransportEnum());
        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());

        assertEquals(exception.getMessage(), "There were no compatible transports on the server.");
    }

    @Test
    public void ClientThatSelectsLongPollingThrowsWhenLongPollingIsNotAvailable() {
        TestHttpClient client = new TestHttpClient().on("POST",
                (req) -> Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                        + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .build();

        assertEquals(TransportEnum.LONG_POLLING, hubConnection.getTransportEnum());
        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());

        assertEquals(exception.getMessage(), "There were no compatible transports on the server.");
    }

    @Test
    public void receivingServerSentEventsTransportFromNegotiateFails() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"ServerSentEvents\",\"transferFormats\":[\"Text\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());

        assertEquals(exception.getMessage(), "There were no compatible transports on the server.");
    }

    @Test
    public void negotiateThatReturnsErrorThrowsFromStart() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", "{\"error\":\"Test error.\"}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withHttpClient(client)
                .withTransportImplementation(transport)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
            () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Test error.", exception.getMessage());
    }

    @Test
    public void DetectWhenTryingToConnectToClassicSignalRServer() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", "{\"Url\":\"/signalr\"," +
                        "\"ConnectionToken\":\"X97dw3uxW4NPPggQsYVcNcyQcuz4w2\"," +
                        "\"ConnectionId\":\"05265228-1e2c-46c5-82a1-6a5bcc3f0143\"," +
                        "\"KeepAliveTimeout\":10.0," +
                        "\"DisconnectTimeout\":5.0," +
                        "\"TryWebSockets\":true," +
                        "\"ProtocolVersion\":\"1.5\"," +
                        "\"TransportConnectTimeout\":30.0," +
                        "\"LongPollDelay\":0.0}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withHttpClient(client)
                .withTransportImplementation(transport)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Detected an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.",
                exception.getMessage());
    }

    @Test
    public void negotiateRedirectIsFollowed()  {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\"}")))
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
    }

    @Test
    public void accessTokenProviderReferenceIsKeptAfterNegotiateRedirect() {
        AtomicReference<String> token = new AtomicReference<>();
        AtomicReference<String> beforeRedirectToken = new AtomicReference<>();

        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> {
                    beforeRedirectToken.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\",\"accessToken\":\"newToken\"}"));
                })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                    token.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.just("User Registered Token"))
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer User Registered Token", beforeRedirectToken.get());
        assertEquals("Bearer newToken", token.get());

        // Clear the tokens to see if they get reset to the proper values
        beforeRedirectToken.set("");
        token.set("");

        // Restart the connection to make sure that the original accessTokenProvider that we registered is still registered before the redirect.
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("Bearer User Registered Token", beforeRedirectToken.get());
        assertEquals("Bearer newToken", token.get());
    }

    @Test
    public void accessTokenProviderIsUsedForNegotiate() {
        AtomicReference<String> token = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            token.set(req.getHeaders().get("Authorization"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.just("secretToken"))
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("Bearer secretToken", token.get());
    }

    @Test
    public void accessTokenProviderIsOverriddenFromRedirectNegotiate() {
        AtomicReference<String> token = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
            .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\",\"accessToken\":\"newToken\"}")))
            .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                token.set(req.getHeaders().get("Authorization"));
                return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\","
                        + "\"connectionToken\":\"connection-token-value\","
                        + "\"negotiateVersion\":1,"
                        + "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
            });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.just("secretToken"))
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://testexample.com/?id=connection-token-value", transport.getUrl());
        hubConnection.stop();
        assertEquals("Bearer newToken", token.get());
    }

    @Test
    public void authorizationHeaderFromNegotiateGetsClearedAfterStopping() {
        AtomicReference<String> token = new AtomicReference<>();
        AtomicReference<String> beforeRedirectToken = new AtomicReference<>();

        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> {
                    beforeRedirectToken.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\",\"accessToken\":\"newToken\"}"));
                })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                    token.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\","
                            + "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer newToken", token.get());

        // Clear the tokens to see if they get reset to the proper values
        beforeRedirectToken.set("");
        token.set("");

        // Restart the connection to make sure that the original accessTokenProvider that we registered is still registered before the redirect.
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertNull(beforeRedirectToken.get());
        assertEquals("Bearer newToken", token.get());
    }

    @Test
    public void authorizationHeaderFromNegotiateGetsSetToNewValue() {
        AtomicReference<String> token = new AtomicReference<>();
        AtomicReference<String> redirectToken = new AtomicReference<>();
        AtomicInteger redirectCount = new AtomicInteger();

        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> {
                    if (redirectCount.get() == 0) {
                        redirectCount.incrementAndGet();
                        redirectToken.set(req.getHeaders().get("Authorization"));
                        return Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\",\"accessToken\":\"firstRedirectToken\"}"));
                    } else {
                        redirectToken.set(req.getHeaders().get("Authorization"));
                        return Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\",\"accessToken\":\"secondRedirectToken\"}"));
                    }
                })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                    token.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer firstRedirectToken", token.get());

        // Clear the tokens to see if they get reset to the proper values
        redirectToken.set("");
        token.set("");

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertNull(redirectToken.get());
        assertEquals("Bearer secondRedirectToken", token.get());
    }

    @Test
    public void connectionTimesOutIfServerDoesNotSendMessage() {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.setServerTimeout(1);
        hubConnection.setTickRate(1);
        SingleSubject<Exception> closedSubject = SingleSubject.create();
        hubConnection.onClosed((e) -> {
            closedSubject.onSuccess(e);
        });

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertEquals("Server timeout elapsed without receiving a message from the server.", closedSubject.timeout(1, TimeUnit.SECONDS).blockingGet().getMessage());
    }

    @Test
    public void connectionSendsPingsRegularly() throws InterruptedException {
        MockTransport mockTransport = new MockTransport(true, false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        hubConnection.setKeepAliveInterval(1);
        hubConnection.setTickRate(1);

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();

        String message = mockTransport.getNextSentMessage().timeout(1, TimeUnit.SECONDS).blockingGet();
        assertEquals("{\"type\":6}" + RECORD_SEPARATOR, message);
        message = mockTransport.getNextSentMessage().timeout(1, TimeUnit.SECONDS).blockingGet();
        assertEquals("{\"type\":6}" + RECORD_SEPARATOR, message);

        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void userAgentHeaderIsSet() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("User-Agent"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();

        assertTrue(header.get().startsWith("Microsoft SignalR/"));
    }

    @Test
    public void userAgentHeaderCanBeOverwritten() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("User-Agent"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("User-Agent", "Updated Value")
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("Updated Value", header.get());
    }

    @Test
    public void userAgentCanBeCleared() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("User-Agent"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("User-Agent", "")
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("", header.get());
    }
    @Test
    public void headersAreSetAndSentThroughBuilder() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("ExampleHeader"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });


        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("ExampleHeader", "ExampleValue")
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("ExampleValue", header.get());
    }

    @Test
    public void headersAreNotClearedWhenConnectionIsRestarted() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("Authorization"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("Authorization", "ExampleValue")
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("ExampleValue", header.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("ExampleValue", header.get());
    }

    @Test
    public void userSetAuthHeaderIsNotClearedAfterRedirect() {
        AtomicReference<String> beforeRedirectHeader  = new AtomicReference<>();
        AtomicReference<String> afterRedirectHeader = new AtomicReference<>();

        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            beforeRedirectHeader.set(req.getHeaders().get("Authorization"));
                            return Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\",\"accessToken\":\"redirectToken\"}\"}"));
                        })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            afterRedirectHeader.set(req.getHeaders().get("Authorization"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("Authorization", "ExampleValue")
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().blockingAwait();
        assertEquals("ExampleValue", beforeRedirectHeader.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("Bearer redirectToken", afterRedirectHeader.get());

        // Making sure you can do this after restarting the HubConnection.
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().blockingAwait();
        assertEquals("ExampleValue", beforeRedirectHeader.get());

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("Bearer redirectToken", afterRedirectHeader.get());
    }

    @Test
    public void sameHeaderSetTwiceGetsOverwritten() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("ExampleHeader"));
                            return Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"));
                        });


        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("ExampleHeader", "ExampleValue")
                .withHeader("ExampleHeader", "New Value")
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("New Value", header.get());
    }

    @Test
    public void hubConnectionCanBeStartedAfterBeingStopped() {
        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .shouldSkipNegotiate(true)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionCanBeStartedAfterBeingStoppedAndRedirected()  {
        MockTransport mockTransport = new MockTransport();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "", "{\"url\":\"http://testexample.com/\"}")))
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "", "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                        + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(mockTransport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void non200FromNegotiateThrowsError() {
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> Single.just(new HttpResponse(500, "Internal server error", "")));

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
            () -> hubConnection.start().timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Unexpected status code returned from negotiate: 500 Internal server error.", exception.getMessage());
    }
}
