// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.util.Iterator;
import java.util.List;
import java.util.concurrent.CancellationException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.extension.ExtendWith;

import ch.qos.logback.classic.spi.ILoggingEvent;
import io.reactivex.rxjava3.core.Completable;
import io.reactivex.rxjava3.core.Observable;
import io.reactivex.rxjava3.core.Single;
import io.reactivex.rxjava3.disposables.Disposable;
import io.reactivex.rxjava3.schedulers.Schedulers;
import io.reactivex.rxjava3.subjects.CompletableSubject;
import io.reactivex.rxjava3.subjects.PublishSubject;
import io.reactivex.rxjava3.subjects.ReplaySubject;
import io.reactivex.rxjava3.subjects.SingleSubject;

@ExtendWith({RxJavaUnhandledExceptionsExtensions.class})
class HubConnectionTest {
    private static final String RECORD_SEPARATOR = "\u001e";
    private static final Type booleanType = (new TypeReference<Boolean>() { }).getType();
    private static final Type doubleType = (new TypeReference<Double>() { }).getType();
    private static final Type integerType = (new TypeReference<Integer>() { }).getType();
    private static final Type stringType = (new TypeReference<String>() { }).getType();

    @Test
    public void checkHubConnectionState() {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void transportCloseTriggersStopInHubConnection() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
        Throwable exception = assertThrows(RuntimeException.class, () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals(TimeoutException.class, exception.getCause().getClass());
        assertEquals("Timed out waiting for the server to respond to the handshake message.", exception.getCause().getMessage());
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void constructHubConnectionWithHttpConnectionOptions() {
        Transport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionClosesAfterCloseMessage() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        mockTransport.receiveMessage("{\"type\":7,\"error\": \"There was an error\"}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionUrlCanBeChanged() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        hubConnection.setBaseUrl("http://newurl.com");
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals("http://newurl.com", hubConnection.getBaseUrl());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void canUpdateUrlInOnClosed() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        hubConnection.onClosed((error) -> {
            hubConnection.setBaseUrl("http://newurl.com");
        });

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals("http://newurl.com", hubConnection.getBaseUrl());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void changingUrlWhenConnectedThrows() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("http://example.com", hubConnection.getBaseUrl());

        Throwable exception = assertThrows(IllegalStateException.class, () -> hubConnection.setBaseUrl("http://newurl.com"));
        assertEquals("The HubConnection must be in the disconnected state to change the url.",exception.getMessage());
    }

    @Test
    public void settingNewUrlToNullThrows() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

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
        mockTransport.getStartTask().timeout(30, TimeUnit.SECONDS).blockingAwait();

        Throwable exception = assertThrows(RuntimeException.class, () -> mockTransport.receiveMessage("{" + RECORD_SEPARATOR));
        assertEquals("An invalid handshake response was received from the server.", exception.getMessage());
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionReceiveHandshakeResponseWithError() {
        MockTransport mockTransport = new MockTransport(false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start();
        mockTransport.getStartTask().timeout(30, TimeUnit.SECONDS).blockingAwait();
        Throwable exception = assertThrows(RuntimeException.class, () ->
            mockTransport.receiveMessage("{\"error\":\"Requested protocol 'messagepack' is not available.\"}" + RECORD_SEPARATOR));
        assertEquals("Error in handshake Requested protocol 'messagepack' is not available.", exception.getMessage());
    }

    @Test
    public void registeringMultipleHandlersAndBothGetTriggered() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> {
            value.getAndUpdate((val) -> val + 1);

            if (value.get() == 2) {
                complete.onComplete();
            }
        };

        hubConnection.on("inc", action);
        hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Double.valueOf(2), value.get());
    }

    @Test
    public void removeHandlerByName() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> {
            value.getAndUpdate((val) -> val + 1);

            complete.onComplete();
        };

        hubConnection.on("inc", action);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Double.valueOf(1), value.get());

        hubConnection.remove("inc");

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that the handler was removed.
        assertEquals(Double.valueOf(0), value.get());
    }

    @Test
    public void removingMultipleHandlersWithOneCallToRemove() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> {
            value.getAndUpdate((val) -> val + 2);

            complete.onComplete();
        };

        hubConnection.on("inc", action);
        hubConnection.on("inc", secondAction);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Double.valueOf(3), value.get());

        hubConnection.remove("inc");

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirm that another invocation doesn't change anything because the handlers have been removed.
        assertEquals(Double.valueOf(3), value.get());
    }

    @Test
    public void removeHandlerWithUnsubscribe() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> {
            value.getAndUpdate((val) -> val + 1);

            complete.onComplete();
        };

        Subscription subscription = hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
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
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> {
            value.getAndUpdate((val) -> val + 1);
            complete.onComplete();
        };

        Subscription subscription = hubConnection.on("inc", action);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
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
        CompletableSubject complete1 = CompletableSubject.create();
        CompletableSubject complete2 = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        Action action = () -> value.getAndUpdate((val) -> val + 1);
        Action secondAction = () -> {
            value.getAndUpdate((val) -> val + 2);
            if (!complete1.hasComplete()) {
                complete1.onComplete();
            } else {
                complete2.onComplete();
            }
        };

        Subscription subscription = hubConnection.on("inc", action);
        hubConnection.on("inc", secondAction);

        assertEquals(Double.valueOf(0), value.get());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        String message = TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]);
        String expectedHanshakeRequest = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;

        assertEquals(expectedHanshakeRequest, message);

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);
        // Confirming that our handler was called and that the counter property was incremented.
        complete1.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Double.valueOf(3), value.get());

        // This removes the first handler so when "inc" is invoked secondAction should still run.
        subscription.unsubscribe();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        complete2.timeout(30, TimeUnit.SECONDS).blockingAwait();
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

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
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<>(0.0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        Action1<Double> action = (number) -> {
            value.getAndUpdate((val) -> val + number);
            if (value.get() == 24.0) {
                complete.onComplete();
            }
        };

        hubConnection.on("add", action, Double.class);
        hubConnection.on("add", action, Double.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"add\",\"arguments\":[12]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Double.valueOf(24), value.get());
    }

    @Test
    public void checkStreamUploadSingleItemThroughSend() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);

        stream.onNext("FirstItem");
        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[3]));
    }

    @Test
    public void checkStreamUploadMultipleStreamsThroughSend() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> firstStream = ReplaySubject.create();
        ReplaySubject<String> secondStream = ReplaySubject.create();

        hubConnection.send("UploadStream", firstStream, secondStream);

        firstStream.onNext("First Stream 1");
        secondStream.onNext("Second Stream 1");
        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First Stream 1\"}\u001E", TestUtils.byteBufferToString(messages[2]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"Second Stream 1\"}\u001E", TestUtils.byteBufferToString(messages[3]));

        firstStream.onComplete();
        secondStream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(6, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[4]));
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", TestUtils.byteBufferToString(messages[5]));
    }

    @Test
    public void checkStreamUploadThroughSendWithArgs() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream, 12);

        stream.onNext("FirstItem");
        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[12],\"streamIds\":[\"1\"]}\u001E", TestUtils.byteBufferToString(messages[1]));
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[3]));
    }

    @Test
    public void useSameSubjectMultipleTimes() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream, stream);

        stream.onNext("FirstItem");
        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"1\",\"2\"]}\u001E", TestUtils.byteBufferToString(messages[1]));
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[3]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(6, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[4]));
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", TestUtils.byteBufferToString(messages[5]));
    }

    @Test
    public void checkStreamUploadSingleItemThroughInvoke() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.invoke(String.class, "UploadStream", stream);

        stream.onNext("FirstItem");
        ByteBuffer[] messages = mockTransport.getSentMessages();
        for (ByteBuffer bb: messages) {
            System.out.println(TestUtils.byteBufferToString(bb));
        }
        assertEquals(3, messages.length);
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"2\"]}\u001E",
                TestUtils.byteBufferToString(messages[1]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", TestUtils.byteBufferToString(messages[3]));
    }

    @Test
    public void checkStreamUploadSingleItemThroughInvokeWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.<String>invoke(stringType, "UploadStream", stream);

        stream.onNext("FirstItem");
        ByteBuffer[] messages = mockTransport.getSentMessages();
        for (ByteBuffer bb: messages) {
            System.out.println(TestUtils.byteBufferToString(bb));
        }
        assertEquals(3, messages.length);

        byte[] firstMessageExpectedBytes = new byte[] { 0x16, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xAC, 0x55, 0x70, 0x6C, 0x6F,
            0x61, 0x64, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6D, (byte) 0x90, (byte) 0x91, (byte) 0xA1, 0x32 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(messages[1]));

        byte[] secondMessageExpectedBytes = new byte[] { 0x0F, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x32, (byte) 0xA9, 0x46, 0x69, 0x72, 0x73,
            0x74, 0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(secondMessageExpectedBytes), ByteString.of(messages[2]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();

        byte[] thirdMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x32, 0x02 };
        assertEquals(ByteString.of(thirdMessageExpectedBytes), ByteString.of(messages[3]));
    }

    @Test
    public void checkStreamUploadSingleItemThroughStream() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.stream(String.class, "UploadStream", stream);

        stream.onNext("FirstItem");

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(3, messages.length);
        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"2\"]}\u001E",
                TestUtils.byteBufferToString(messages[1]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", TestUtils.byteBufferToString(messages[3]));
    }

    @Test
    public void checkStreamUploadSingleItemThroughStreamWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.<String>stream(stringType, "UploadStream", stream);

        stream.onNext("FirstItem");

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(3, messages.length);

        byte[] firstMessageExpectedBytes = new byte[] { 0x16, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xAC, 0x55, 0x70, 0x6C, 0x6F,
            0x61, 0x64, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6D, (byte) 0x90, (byte) 0x91, (byte) 0xA1, 0x32 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(messages[1]));

        byte[] secondMessageExpectedBytes = new byte[] { 0x0F, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x32, (byte) 0xA9, 0x46, 0x69, 0x72, 0x73,
            0x74, 0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(secondMessageExpectedBytes), ByteString.of(messages[2]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);

        byte[] thirdMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x32, 0x02 };
        assertEquals(ByteString.of(thirdMessageExpectedBytes), ByteString.of(messages[3]));
    }

    @Test
    public void useSameSubjectInMutlipleStreamsFromDifferentMethods() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);
        hubConnection.invoke(String.class, "UploadStream", stream);
        hubConnection.stream(String.class, "UploadStream", stream);

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":1,\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"1\"]}\u001E", TestUtils.byteBufferToString(messages[1]));
        assertEquals("{\"type\":1,\"invocationId\":\"2\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"3\"]}\u001E",
                TestUtils.byteBufferToString(messages[2]));
        assertEquals("{\"type\":4,\"invocationId\":\"4\",\"target\":\"UploadStream\",\"arguments\":[],\"streamIds\":[\"5\"]}\u001E",
                TestUtils.byteBufferToString(messages[3]));

        stream.onNext("FirstItem");

        messages = mockTransport.getSentMessages();
        assertEquals(7, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[4]));
        assertEquals("{\"type\":2,\"invocationId\":\"3\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[5]));
        assertEquals("{\"type\":2,\"invocationId\":\"5\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[6]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(10, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[7]));
        assertEquals("{\"type\":3,\"invocationId\":\"3\"}\u001E", TestUtils.byteBufferToString(messages[8]));
        assertEquals("{\"type\":3,\"invocationId\":\"5\"}\u001E", TestUtils.byteBufferToString(messages[9]));
    }

    @Test
    public void useSameSubjectInMutlipleStreamsFromDifferentMethodsWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);
        hubConnection.<String>invoke(stringType, "UploadStream", stream);
        hubConnection.<String>stream(stringType, "UploadStream", stream);

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);

        byte[] firstMessageExpectedBytes = new byte[] { 0x15, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xAC, 0x55, 0x70, 0x6C, 0x6F, 0x61,
            0x64, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6D, (byte) 0x90, (byte) 0x91, (byte) 0xA1, 0x31 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(messages[1]));

        byte[] secondMessageExpectedBytes = new byte[] { 0x16, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x32, (byte) 0xAC, 0x55, 0x70, 0x6C, 0x6F,
            0x61, 0x64, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6D, (byte) 0x90, (byte) 0x91, (byte) 0xA1, 0x33 };
        assertEquals(ByteString.of(secondMessageExpectedBytes), ByteString.of(messages[2]));

        byte[] thirdMessageExpectedBytes = new byte[] { 0x16, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x34, (byte) 0xAC, 0x55, 0x70, 0x6C, 0x6F,
            0x61, 0x64, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6D, (byte) 0x90, (byte) 0x91, (byte) 0xA1, 0x35 };
        assertEquals(ByteString.of(thirdMessageExpectedBytes), ByteString.of(messages[3]));

        stream.onNext("FirstItem");

        messages = mockTransport.getSentMessages();
        assertEquals(7, messages.length);

        byte[] fourthMessageExpectedBytes = new byte[] { 0x0F, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA9, 0x46, 0x69, 0x72, 0x73, 0x74,
            0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(fourthMessageExpectedBytes), ByteString.of(messages[4]));

        byte[] fifthMessageExpectedBytes = new byte[] { 0x0F, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x33, (byte) 0xA9, 0x46, 0x69, 0x72, 0x73, 0x74,
                0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(fifthMessageExpectedBytes), ByteString.of(messages[5]));

        byte[] sixthMessageExpectedBytes = new byte[] { 0x0F, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x35, (byte) 0xA9, 0x46, 0x69, 0x72, 0x73, 0x74,
                0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(sixthMessageExpectedBytes), ByteString.of(messages[6]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(10, messages.length);

        byte[] seventhMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x02 };
        assertEquals(ByteString.of(seventhMessageExpectedBytes), ByteString.of(messages[7]));

        byte[] eighthMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x33, 0x02 };
        assertEquals(ByteString.of(eighthMessageExpectedBytes), ByteString.of(messages[8]));

        byte[] ninthMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x35, 0x02 };
        assertEquals(ByteString.of(ninthMessageExpectedBytes), ByteString.of(messages[9]));
    }

    @Test
    public void streamUploadCallOnError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onError(new RuntimeException("onError called"));
        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));
        assertEquals("{\"type\":3,\"invocationId\":\"1\",\"error\":\"java.lang.RuntimeException: onError called\"}\u001E",
                TestUtils.byteBufferToString(messages[3]));

        // onComplete doesn't send a completion message after onError.
        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
    }

    @Test
    public void checkStreamUploadMultipleItemsThroughSend() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.send("UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onNext("SecondItem");
        stream.onNext("ThirdItem");

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(5, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"SecondItem\"}\u001E", TestUtils.byteBufferToString(messages[3]));
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"ThirdItem\"}\u001E", TestUtils.byteBufferToString(messages[4]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(6, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[5]));
    }

    @Test
    public void checkStreamUploadMultipleItemsThroughInvoke() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.invoke(String.class, "UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onNext("SecondItem");

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"FirstItem\"}\u001E", TestUtils.byteBufferToString(messages[2]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"SecondItem\"}\u001E", TestUtils.byteBufferToString(messages[3]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(5, messages.length);
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", TestUtils.byteBufferToString(messages[4]));
    }

    @Test
    public void checkStreamUploadMultipleItemsThroughInvokeWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ReplaySubject<String> stream = ReplaySubject.create();
        hubConnection.<String>invoke(stringType, "UploadStream", stream);

        stream.onNext("FirstItem");
        stream.onNext("SecondItem");

        ByteBuffer[] messages = mockTransport.getSentMessages();
        assertEquals(4, messages.length);

        byte[] firstMessageExpectedBytes = new byte[] { 0x0F, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x32, (byte) 0xA9, 0x46, 0x69, 0x72, 0x73, 0x74,
            0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(messages[2]));

        byte[] secondMessageExpectedBytes = new byte[] { 0x10, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x32, (byte) 0xAA, 0x53, 0x65, 0x63, 0x6F, 0x6E,
            0x64, 0x49, 0x74, 0x65, 0x6D };
        assertEquals(ByteString.of(secondMessageExpectedBytes), ByteString.of(messages[3]));

        stream.onComplete();
        messages = mockTransport.getSentMessages();
        assertEquals(5, messages.length);

        byte[] thirdMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x32, 0x02 };
        assertEquals(ByteString.of(thirdMessageExpectedBytes), ByteString.of(messages[4]));
    }

    @Test
    public void canStartAndStopMultipleStreams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

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
        ByteBuffer[] messages = mockTransport.getSentMessages();

        // Handshake message + 2 calls to send + 4 calls to onNext + 2 calls to onComplete = 9
        assertEquals(9, messages.length);
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Stream One First Item\"}\u001E", TestUtils.byteBufferToString(messages[3]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"Stream Two First Item\"}\u001E", TestUtils.byteBufferToString(messages[4]));
        assertEquals("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Stream One Second Item\"}\u001E", TestUtils.byteBufferToString(messages[5]));
        assertEquals("{\"type\":2,\"invocationId\":\"2\",\"item\":\"Stream Two Second Item\"}\u001E", TestUtils.byteBufferToString(messages[6]));
        assertEquals("{\"type\":3,\"invocationId\":\"1\"}\u001E", TestUtils.byteBufferToString(messages[7]));
        assertEquals("{\"type\":3,\"invocationId\":\"2\"}\u001E", TestUtils.byteBufferToString(messages[8]));
    }

    @Test
    public void checkStreamSingleItem() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> {},
                () -> completed.set(true));

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());
        assertFalse(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        assertTrue(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"hello\"}" + RECORD_SEPARATOR);
        assertTrue(completed.get());

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());
    }

    @Test
    public void checkStreamSingleItemWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> {},
                () -> completed.set(true));

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());
        assertFalse(onNextCalled.get());

        byte[] secondMessageExpectedBytes = new byte[] { 0x0B, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA5, 0x46, 0x69, 0x72, 0x73, 0x74 };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondMessageExpectedBytes));

        assertTrue(onNextCalled.get());

        byte[] thirdMessageExpectedBytes = new byte[] { 0x0C, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, (byte) 0xA5, 0x68, 0x65, 0x6C, 0x6C, 0x6F };
        mockTransport.receiveMessage(ByteBuffer.wrap(thirdMessageExpectedBytes));
        assertTrue(completed.get());

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());
    }

    @Test
    public void checkStreamCompletionResult() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> {},
                () -> completed.set(true));

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());
        assertFalse(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        assertTrue(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"COMPLETED\"}" + RECORD_SEPARATOR);
        assertTrue(completed.get());

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());
        assertEquals("COMPLETED", result.timeout(30, TimeUnit.SECONDS).blockingLast());
    }

    @Test
    public void checkStreamCompletionResultWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> {},
                () -> completed.set(true));

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());
        assertFalse(onNextCalled.get());

        byte[] secondMessageExpectedBytes = new byte[] { 0x0B, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA5, 0x46, 0x69, 0x72, 0x73, 0x74 };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondMessageExpectedBytes));

        assertTrue(onNextCalled.get());

        byte[] thirdMessageExpectedBytes = new byte[] { 0x10, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, (byte) 0xA9, 0x43, 0x4F, 0x4D, 0x50,
            0x4C, 0x45, 0x54, 0x45, 0x44 };
        mockTransport.receiveMessage(ByteBuffer.wrap(thirdMessageExpectedBytes));
        assertTrue(completed.get());

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());
        assertEquals("COMPLETED", result.timeout(30, TimeUnit.SECONDS).blockingLast());
    }

    @Test
    public void checkStreamCompletionError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean onErrorCalled = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> onErrorCalled.set(true),
                () -> {});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(onErrorCalled.get());
        assertFalse(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        assertTrue(onNextCalled.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);
        assertTrue(onErrorCalled.get());

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());
        Throwable exception = assertThrows(HubException.class, () -> result.timeout(30, TimeUnit.SECONDS).blockingLast());
        assertEquals("There was an error", exception.getMessage());
    }

    @Test
    public void checkStreamCompletionErrorWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean onErrorCalled = new AtomicBoolean();
        AtomicBoolean onNextCalled = new AtomicBoolean();
        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        result.subscribe((item) -> onNextCalled.set(true),
                (error) -> onErrorCalled.set(true),
                () -> {});

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(onErrorCalled.get());
        assertFalse(onNextCalled.get());

        byte[] secondMessageExpectedBytes = new byte[] { 0x0B, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA5, 0x46, 0x69, 0x72, 0x73, 0x74 };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondMessageExpectedBytes));

        assertTrue(onNextCalled.get());

        byte[] thirdMessageExpectedBytes = new byte[] { 0x0C, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x01, (byte) 0xA5, 0x45, 0x72, 0x72, 0x6F, 0x72 };
        mockTransport.receiveMessage(ByteBuffer.wrap(thirdMessageExpectedBytes));
        assertTrue(onErrorCalled.get());

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());
        Throwable exception = assertThrows(HubException.class, () -> result.timeout(30, TimeUnit.SECONDS).blockingLast());
        assertEquals("Error", exception.getMessage());
    }

    @Test
    public void checkStreamMultipleItems() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Second\"}" + RECORD_SEPARATOR);
        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"null\"}" + RECORD_SEPARATOR);

        Iterator<String> resultIterator = result.timeout(30, TimeUnit.SECONDS).blockingIterable().iterator();
        assertEquals("First", resultIterator.next());
        assertEquals("Second", resultIterator.next());
        assertTrue(completed.get());
    }

    @Test
    public void checkStreamMultipleItemsWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());

        byte[] secondMessageExpectedBytes = new byte[] { 0x0B, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA5, 0x46, 0x69, 0x72, 0x73, 0x74 };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondMessageExpectedBytes));

        byte[] thirdMessageExpectedBytes = new byte[] { 0x0C, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA6, 0x53, 0x65, 0x63, 0x6F, 0x6E, 0x64 };
        mockTransport.receiveMessage(ByteBuffer.wrap(thirdMessageExpectedBytes));

        byte[] fourthMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x02 };
        mockTransport.receiveMessage(ByteBuffer.wrap(fourthMessageExpectedBytes));

        Iterator<String> resultIterator = result.timeout(30, TimeUnit.SECONDS).blockingIterable().iterator();
        assertEquals("First", resultIterator.next());
        assertEquals("Second", resultIterator.next());
        assertTrue(completed.get());
    }

    @Test
    public void checkCancelIsSentAfterDispose() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());

        subscription.dispose();
        assertEquals("{\"type\":5,\"invocationId\":\"1\"}" + RECORD_SEPARATOR, TestUtils.byteBufferToString(mockTransport.getSentMessages()[2]));
    }

    @Test
    public void checkCancelIsSentAfterDisposeWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());

        subscription.dispose();
        assertEquals(ByteString.of(new byte[] { 0x05, (byte) 0x93, 0x05, (byte) 0x80, (byte) 0xA1, 0x31 }),
            ByteString.of(mockTransport.getSentMessages()[2]));
    }

    @Test
    public void checkCancelIsSentAfterAllSubscriptionsAreDisposed() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

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
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[mockTransport.getSentMessages().length - 1]));

        secondSubscription.dispose();
        assertEquals(3, mockTransport.getSentMessages().length);
        assertEquals("{\"type\":5,\"invocationId\":\"1\"}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[mockTransport.getSentMessages().length - 1]));
    }

    @Test
    public void checkCancelIsSentAfterAllSubscriptionsAreDisposedWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        Disposable secondSubscription = result.subscribe((item) -> {/*OnNext*/ },
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        subscription.dispose();
        assertEquals(2, mockTransport.getSentMessages().length);

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[mockTransport.getSentMessages().length - 1]));

        secondSubscription.dispose();
        assertEquals(3, mockTransport.getSentMessages().length);
        assertEquals(ByteString.of(new byte[] { 0x05, (byte) 0x93, 0x05, (byte) 0x80, (byte) 0xA1, 0x31 }),
                ByteString.of(mockTransport.getSentMessages()[mockTransport.getSentMessages().length - 1]));
    }

    @Test
    public void checkStreamWithDispose() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        subscription.dispose();
        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Second\"}" + RECORD_SEPARATOR);

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingLast());
    }

    @Test
    public void checkStreamWithDisposeWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));

        byte[] secondMessageExpectedBytes = new byte[] { 0x0B, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA5, 0x46, 0x69, 0x72, 0x73, 0x74 };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondMessageExpectedBytes));

        subscription.dispose();
        byte[] thirdMessageExpectedBytes = new byte[] { 0x0C, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA6, 0x53, 0x65, 0x63, 0x6F, 0x6E, 0x64 };
        mockTransport.receiveMessage(ByteBuffer.wrap(thirdMessageExpectedBytes));

        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingLast());
    }

    @Test
    public void checkStreamWithDisposeWithMultipleSubscriptions() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.stream(String.class, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        Disposable subscription2 = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        assertEquals("{\"type\":4,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());

        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"First\"}" + RECORD_SEPARATOR);

        subscription.dispose();
        mockTransport.receiveMessage("{\"type\":2,\"invocationId\":\"1\",\"item\":\"Second\"}" + RECORD_SEPARATOR);

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\"}" + RECORD_SEPARATOR);
        assertTrue(completed.get());
        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());

        subscription2.dispose();
        assertEquals("Second", result.timeout(30, TimeUnit.SECONDS).blockingLast());
    }

    @Test
    public void checkStreamWithDisposeWithMultipleSubscriptionsWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean completed = new AtomicBoolean();
        Observable<String> result = hubConnection.<String>stream(stringType, "echo", "message");
        Disposable subscription = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/});

        Disposable subscription2 = result.subscribe((item) -> {/*OnNext*/},
                (error) -> {/*OnError*/},
                () -> {/*OnCompleted*/completed.set(true);});

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(completed.get());

        byte[] secondMessageExpectedBytes = new byte[] { 0x0B, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA5, 0x46, 0x69, 0x72, 0x73, 0x74 };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondMessageExpectedBytes));

        subscription.dispose();
        byte[] thirdMessageExpectedBytes = new byte[] { 0x0C, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA6, 0x53, 0x65, 0x63, 0x6F, 0x6E, 0x64 };
        mockTransport.receiveMessage(ByteBuffer.wrap(thirdMessageExpectedBytes));

        byte[] fourthMessageExpectedBytes = new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x02 };
        mockTransport.receiveMessage(ByteBuffer.wrap(fourthMessageExpectedBytes));

        assertTrue(completed.get());
        assertEquals("First", result.timeout(30, TimeUnit.SECONDS).blockingFirst());

        subscription2.dispose();
        assertEquals("Second", result.timeout(30, TimeUnit.SECONDS).blockingLast());
    }

    @Test
    public void invokeWaitsForCompletionMessage() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(Integer.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertEquals(Integer.valueOf(42), result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void invokeWaitsForCompletionMessageWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.<Integer>invoke(integerType, "echo", "message");
        result.doOnSuccess(value -> done.set(true)).subscribe();

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage(ByteBuffer.wrap(new byte[] { 0x07, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, 0x2A }));

        assertEquals(Integer.valueOf(42), result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void invokeNoReturnValueWaitsForCompletion() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe();

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\"}" + RECORD_SEPARATOR);

        assertTrue(result.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(done.get());
    }

    @Test
    public void invokeNoReturnValueWaitsForCompletionWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe();

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage(ByteBuffer.wrap(new byte[] { 0x06, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x02 }));

        assertTrue(result.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(done.get());
    }

    @Test
    public void invokeCompletedByCompletionMessageWithResult() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe();

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertTrue(result.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(done.get());
    }

    @Test
    public void invokeCompletedByCompletionMessageWithResultWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe();

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage(ByteBuffer.wrap(new byte[] { 0x07, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, 0x2A }));

        assertTrue(result.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(done.get());
    }

    @Test
    public void completionWithResultAndErrorHandlesError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe(() -> {}, (error) -> {});

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        Throwable exception = assertThrows(IllegalArgumentException.class, () ->
            mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42,\"error\":\"There was an error\"}" + RECORD_SEPARATOR));
        assertEquals("Expected either 'error' or 'result' to be provided, but not both.", exception.getMessage());
    }

    @Test
    public void invokeNoReturnValueHandlesError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe(() -> {}, (error) -> {});

        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"test\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);

        assertTrue(result.onErrorComplete().blockingAwait(30, TimeUnit.SECONDS));

        AtomicReference<String> errorMessage = new AtomicReference<>();
        result.doOnError(error -> {
            errorMessage.set(error.getMessage());
        }).subscribe(() -> {}, (error) -> {});

        assertEquals("There was an error", errorMessage.get());
    }

    @Test
    public void invokeNoReturnValueHandlesErrorWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Completable result = hubConnection.invoke("test", "message");
        result.doOnComplete(() -> done.set(true)).subscribe(() -> {}, (error) -> {});

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74,
                (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
            assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        byte[] completionMessageErrorBytes = new byte[] { 0x19, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x01, (byte) 0xB2, 0x54, 0x68, 0x65,
                0x72, 0x65, 0x20, 0x77, 0x61, 0x73, 0x20, 0x61, 0x6E, 0x20, 0x65, 0x72, 0x72, 0x6F, 0x72 };
        mockTransport.receiveMessage(ByteBuffer.wrap(completionMessageErrorBytes));

        assertTrue(result.onErrorComplete().blockingAwait(30, TimeUnit.SECONDS));

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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<String> result = hubConnection.invoke(String.class, "fixedMessage", (Object)null);
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"fixedMessage\",\"arguments\":[null]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"Hello World\"}" + RECORD_SEPARATOR);

        assertEquals("Hello World", result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void canSendNullArgInInvocationWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<String> result = hubConnection.<String>invoke(stringType, "fixedMessage", (Object)null);
        result.doOnSuccess(value -> done.set(true)).subscribe();

        byte[] firstMessageExpectedBytes = new byte[] { 0x15, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xAC, 0x66, 0x69, 0x78, 0x65, 0x64,
            0x4D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x91, (byte) 0xC0, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        byte[] completionMessageBytes = new byte[] { 0x12, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, (byte) 0xAB, 0x48, 0x65, 0x6C, 0x6C,
            0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 };
        mockTransport.receiveMessage(ByteBuffer.wrap(completionMessageBytes));

        assertEquals("Hello World", result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void canSendMultipleNullArgsInInvocation() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<String> result = hubConnection.invoke(String.class, "fixedMessage", null, null);
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"fixedMessage\",\"arguments\":[null,null]}"+ RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":\"Hello World\"}" + RECORD_SEPARATOR);

        assertEquals("Hello World", result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void canSendMultipleNullArgsInInvocationWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<String> result = hubConnection.invoke(String.class, "fixedMessage", null, null);
        result.doOnSuccess(value -> done.set(true)).subscribe();

        byte[] firstMessageExpectedBytes = new byte[] { 0x16, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xAC, 0x66, 0x69, 0x78, 0x65, 0x64, 0x4D,
            0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x92, (byte) 0xC0, (byte) 0xC0, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));
        assertFalse(done.get());

        byte[] completionMessageBytes = new byte[] { 0x12, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, (byte) 0xAB, 0x48, 0x65, 0x6C, 0x6C,
            0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 };
        mockTransport.receiveMessage(ByteBuffer.wrap(completionMessageBytes));

        assertEquals("Hello World", result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void multipleInvokesWaitForOwnCompletionMessage() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean doneFirst = new AtomicBoolean();
        AtomicBoolean doneSecond = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(Integer.class, "echo", "message");
        Single<String> result2 = hubConnection.invoke(String.class, "echo", "message");
        result.doOnSuccess(value -> doneFirst.set(true)).subscribe();
        result2.doOnSuccess(value -> doneSecond.set(true)).subscribe();
        assertEquals("{\"type\":1,\"invocationId\":\"1\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[1]));
        assertEquals("{\"type\":1,\"invocationId\":\"2\",\"target\":\"echo\",\"arguments\":[\"message\"]}" + RECORD_SEPARATOR,
                TestUtils.byteBufferToString(mockTransport.getSentMessages()[2]));
        assertFalse(doneFirst.get());
        assertFalse(doneSecond.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"2\",\"result\":\"message\"}" + RECORD_SEPARATOR);
        assertEquals("message", result2.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertFalse(doneFirst.get());
        assertTrue(doneSecond.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);
        assertEquals(Integer.valueOf(42), result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(doneFirst.get());
    }

    @Test
    public void multipleInvokesWaitForOwnCompletionMessageWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean doneFirst = new AtomicBoolean();
        AtomicBoolean doneSecond = new AtomicBoolean();
        Single<Integer> result = hubConnection.<Integer>invoke(integerType, "echo", "message");
        Single<String> result2 = hubConnection.<String>invoke(stringType, "echo", "message");
        result.doOnSuccess(value -> doneFirst.set(true)).subscribe();
        result2.doOnSuccess(value -> doneSecond.set(true)).subscribe();

        byte[] firstMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x31, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
            (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(firstMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[1]));

        byte[] secondMessageExpectedBytes = new byte[] { 0x14, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xA1, 0x32, (byte) 0xA4, 0x65, 0x63, 0x68, 0x6F,
                (byte) 0x91, (byte) 0xA7, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, (byte) 0x90 };
        assertEquals(ByteString.of(secondMessageExpectedBytes), ByteString.of(mockTransport.getSentMessages()[2]));
        assertFalse(doneFirst.get());
        assertFalse(doneSecond.get());

        byte[] firstCompletionMessageBytes = new byte[] { 0x0E, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x32, 0x03, (byte) 0xA7, 0x6D, 0x65, 0x73,
            0x73, 0x61, 0x67, 0x65 };
        mockTransport.receiveMessage(ByteBuffer.wrap(firstCompletionMessageBytes));
        assertEquals("message", result2.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertFalse(doneFirst.get());
        assertTrue(doneSecond.get());

        byte[] secondCompletionMessageBytes = new byte[] { 0x07, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, 0x2A };
        mockTransport.receiveMessage(ByteBuffer.wrap(secondCompletionMessageBytes));
        assertEquals(Integer.valueOf(42), result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(doneFirst.get());
    }

    @Test
    public void invokeWorksForPrimitiveTypes() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        // int.class is a primitive type and since we use Class.cast to cast an Object to the expected return type
        // which does not work for primitives we have to write special logic for that case.
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"result\":42}" + RECORD_SEPARATOR);

        assertEquals(Integer.valueOf(42), result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void invokeWorksForPrimitiveTypesWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        // int.class is a primitive type and since we use Class.cast to cast an Object to the expected return type
        // which does not work for primitives we have to write special logic for that case.
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true)).subscribe();
        assertFalse(done.get());

        mockTransport.receiveMessage(ByteBuffer.wrap(new byte[] { 0x07, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x03, 0x2A }));

        assertEquals(Integer.valueOf(42), result.timeout(30, TimeUnit.SECONDS).blockingGet());
        assertTrue(done.get());
    }

    @Test
    public void completionMessageCanHaveError() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true));
        assertFalse(done.get());

        mockTransport.receiveMessage("{\"type\":3,\"invocationId\":\"1\",\"error\":\"There was an error\"}" + RECORD_SEPARATOR);

        String exceptionMessage = null;
        try {
            result.timeout(30, TimeUnit.SECONDS).blockingGet();
            assertFalse(true);
        } catch (HubException ex) {
            exceptionMessage = ex.getMessage();
        }

        assertEquals("There was an error", exceptionMessage);
    }

    @Test
    public void completionMessageCanHaveErrorWithMessagePack() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true));
        assertFalse(done.get());

        byte[] completionMessageErrorBytes = new byte[] { 0x19, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA1, 0x31, 0x01, (byte) 0xB2, 0x54, 0x68, 0x65,
            0x72, 0x65, 0x20, 0x77, 0x61, 0x73, 0x20, 0x61, 0x6E, 0x20, 0x65, 0x72, 0x72, 0x6F, 0x72 };
        mockTransport.receiveMessage(ByteBuffer.wrap(completionMessageErrorBytes));

        String exceptionMessage = null;
        try {
            result.timeout(30, TimeUnit.SECONDS).blockingGet();
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        AtomicBoolean done = new AtomicBoolean();
        Single<Integer> result = hubConnection.invoke(int.class, "echo", "message");
        result.doOnSuccess(value -> done.set(true));
        assertFalse(done.get());

        hubConnection.stop();

        RuntimeException hasException = null;
        try {
            result.timeout(30, TimeUnit.SECONDS).blockingGet();
            assertFalse(true);
        } catch (CancellationException ex) {
            hasException = ex;
        }

        assertEquals("Invocation was canceled.", hasException.getMessage());
    }

    @Test
    public void sendWithNoParamsTriggersOnHandler() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Integer> value = new AtomicReference<>(0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", () ->{
            value.getAndUpdate((val) -> val + 1);

            complete.onComplete();
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Integer.valueOf(1), value.get());
    }

    @Test
    public void sendWithParamTriggersOnHandler() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value = new AtomicReference<>();
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param) -> {
            value.set(param);

            complete.onComplete();
        }, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\"]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "Hello World");

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Hello World", value.get());
    }

    @Test
    public void sendWithTwoParamsTriggersOnHandler() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<Double> value2 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2) -> {
            value1.set(param1);
            value2.set(param2);

            complete.onComplete();
        }, String.class, double.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\", 12]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "Hello World", 12);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Hello World", value1.get());
        assertEquals(12d, value2.get().doubleValue());
    }

    @Test
    public void sendWithThreeParamsTriggersOnHandler() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);

            complete.onComplete();
        }, String.class, String.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\"]}" + RECORD_SEPARATOR);
        hubConnection.send("inc", "A", "B", "C");

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
    }

    @Test
    public void sendWithFourParamsTriggersOnHandler() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<String> value4 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3, param4) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);

            complete.onComplete();
        }, String.class, String.class, String.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\", \"D\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertEquals("D", value4.get());
    }

    @Test
    public void sendWithFiveParamsTriggersOnHandler()  {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3, param4, param5) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);

            complete.onComplete();
        }, String.class, String.class, String.class, boolean.class, double.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12 ]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get().booleanValue());
        assertEquals(12d, value5.get().doubleValue());
    }

    @Test
    public void sendWithSixParamsTriggersOnHandler()  {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1, param2, param3, param4, param5, param6) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);

            complete.onComplete();
        }, String.class, String.class, String.class, boolean.class, double.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12,\"D\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get().booleanValue());
        assertEquals(12d, value5.get().doubleValue());
        assertEquals("D", value6.get());
    }

    @Test
    public void sendWithSevenParamsTriggersOnHandler()  {
        CompletableSubject complete = CompletableSubject.create();
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
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
            value7.set(param7);

            complete.onComplete();
        }, String.class, String.class, String.class, boolean.class, double.class, String.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12,\"D\",\"E\"]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get().booleanValue());
        assertEquals(12d, value5.get().doubleValue());
        assertEquals("D", value6.get());
        assertEquals("E", value7.get());
    }

    @Test
    public void sendWithEightParamsTriggersOnHandler()  {
        CompletableSubject complete = CompletableSubject.create();
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
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
            value7.set(param7);
            value8.set(param8);

            complete.onComplete();
        }, String.class, String.class, String.class, boolean.class, double.class, String.class, String.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"A\", \"B\", \"C\",true,12,\"D\",\"E\",\"F\"]}" + RECORD_SEPARATOR);
        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get().booleanValue());
        assertEquals(12d, value5.get().doubleValue());
        assertEquals("D", value6.get());
        assertEquals("E", value7.get());
        assertEquals("F", value8.get());
    }

    @Test
    public void sendWithNoParamsTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Integer> value = new AtomicReference<>(0);
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.on("inc", () -> {
            value.getAndUpdate((val) -> val + 1);

            complete.onComplete();
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x0A, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x90, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Integer.valueOf(1), value.get());
    }

    @Test
    public void sendWithParamTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value = new AtomicReference<>();
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String>on("inc", (param) -> {
            value.set(param);

            complete.onComplete();
        }, stringType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x91, (byte) 0xA1,
            0x41, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));
        hubConnection.send("inc", "A");

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value.get());
    }

    @Test
    public void sendWithTwoParamsTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<Double> value2 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, Double>on("inc", (param1, param2) -> {
            value1.set(param1);
            value2.set(param2);

            complete.onComplete();
        }, stringType, doubleType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x15, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x92, (byte) 0xA1, 0x41,
            (byte) 0xCB, 0x40, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));
        hubConnection.send("inc", "A", 12);

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals(Double.valueOf(12), value2.get());
    }

    @Test
    public void sendWithThreeParamsTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, String, String>on("inc", (param1, param2, param3) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);

            complete.onComplete();
        }, stringType, stringType, stringType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x10, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x93, (byte) 0xA1, 0x41,
            (byte) 0xA1, 0x42, (byte) 0xA1, 0x43, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));
        hubConnection.send("inc", "A", "B", "C");

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
    }

    @Test
    public void sendWithFourParamsTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<String> value4 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, String, String, String>on("inc", (param1, param2, param3, param4) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);

            complete.onComplete();
        }, stringType, stringType, stringType, stringType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x12, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x94, (byte) 0xA1, 0x41,
                (byte) 0xA1, 0x42, (byte) 0xA1, 0x43, (byte) 0xA1, 0x44, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertEquals("D", value4.get());
    }

    @Test
    public void sendWithFiveParamsTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, String, String, Boolean, Double>on("inc", (param1, param2, param3, param4, param5) ->{
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);

            complete.onComplete();
        }, stringType, stringType, stringType, booleanType, doubleType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x1A, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x95, (byte) 0xA1, 0x41,
            (byte) 0xA1, 0x42, (byte) 0xA1, 0x43, (byte) 0xC3, (byte) 0xCB, 0x40, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
    }

    @Test
    public void sendWithSixParamsTriggersOnHandlerWithMessagePack() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, String, String, Boolean, Double, String>on("inc", (param1, param2, param3, param4, param5, param6) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);

            complete.onComplete();
        }, stringType, stringType, stringType, booleanType, doubleType, stringType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x1C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x96, (byte) 0xA1, 0x41,
            (byte) 0xA1, 0x42, (byte) 0xA1, 0x43, (byte) 0xC3, (byte) 0xCB, 0x40, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0xA1, 0x44, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
        assertEquals("D", value6.get());
    }

    @Test
    public void sendWithSevenParamsTriggersOnHandlerWithMessagePack()  {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();
        AtomicReference<String> value7 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, String, String, Boolean, Double, String, String>on("inc", (param1, param2, param3, param4, param5, param6, param7) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
            value7.set(param7);

            complete.onComplete();
        }, stringType, stringType, stringType, booleanType, doubleType, stringType, stringType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x1E, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x97, (byte) 0xA1, 0x41,
            (byte) 0xA1, 0x42, (byte) 0xA1, 0x43, (byte) 0xC3, (byte) 0xCB, 0x40, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0xA1, 0x44, (byte) 0xA1,
            0x45, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("A", value1.get());
        assertEquals("B", value2.get());
        assertEquals("C", value3.get());
        assertTrue(value4.get());
        assertEquals(Double.valueOf(12), value5.get());
        assertEquals("D", value6.get());
        assertEquals("E", value7.get());
    }

    @Test
    public void sendWithEightParamsTriggersOnHandlerWithMessagePack()  {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        AtomicReference<String> value3 = new AtomicReference<>();
        AtomicReference<Boolean> value4 = new AtomicReference<>();
        AtomicReference<Double> value5 = new AtomicReference<>();
        AtomicReference<String> value6 = new AtomicReference<>();
        AtomicReference<String> value7 = new AtomicReference<>();
        AtomicReference<String> value8 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<String, String, String, Boolean, Double, String, String, String>on("inc", (param1, param2, param3, param4, param5, param6, param7, param8) -> {
            value1.set(param1);
            value2.set(param2);
            value3.set(param3);
            value4.set(param4);
            value5.set(param5);
            value6.set(param6);
            value7.set(param7);
            value8.set(param8);

            complete.onComplete();
        }, stringType, stringType, stringType, booleanType, doubleType, stringType, stringType, stringType);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x20, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x98, (byte) 0xA1, 0x41,
            (byte) 0xA1, 0x42, (byte) 0xA1, 0x43, (byte) 0xC3, (byte) 0xCB, 0x40, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0xA1, 0x44, (byte) 0xA1,
            0x45, (byte) 0xA1, 0x46, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));
        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
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
        SingleSubject<Custom> value1 = SingleSubject.create();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", (param1) -> {
            value1.onSuccess(param1);
        }, Custom.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[{\"number\":1,\"str\":\"A\",\"bools\":[true,false]}]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and the correct message was passed in.
        Custom custom = value1.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals(1, custom.number);
        assertEquals("A", custom.str);
        assertEquals(2, custom.bools.length);
        assertEquals(true, custom.bools[0]);
        assertEquals(false, custom.bools[1]);
    }

    @Test
    public void sendWithCustomObjectTriggersOnHandlerWithMessagePack()  {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<PersonPojo<Short>> value1 = new AtomicReference<>();

        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport, true);

        hubConnection.<PersonPojo<Short>>on("inc", (param1) -> {
            value1.set(param1);

            complete.onComplete();
        }, (new TypeReference<PersonPojo<Short>>() { }).getType());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        byte[] messageBytes = new byte[] { 0x2F, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x69, 0x6E, 0x63, (byte) 0x91, (byte) 0x84,
            (byte) 0xA9, 0x66, 0x69, 0x72, 0x73, 0x74, 0x4E, 0x61, 0x6D, 0x65, (byte) 0xA4, 0x4A, 0x6F, 0x68, 0x6E, (byte) 0xA8, 0x6C, 0x61, 0x73, 0x74,
            0x4E, 0x61, 0x6D, 0x65, (byte) 0xA3, 0x44, 0x6F, 0x65, (byte) 0xA3, 0x61, 0x67, 0x65, 0x1E, (byte) 0xA1, 0x74, 0x05, (byte) 0x90 };
        mockTransport.receiveMessage(ByteBuffer.wrap(messageBytes));

        // Confirming that our handler was called and the correct message was passed in.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        PersonPojo<Short> person = value1.get();
        assertEquals("John", person.getFirstName());
        assertEquals("Doe", person.getLastName());
        assertEquals(30, person.getAge());
        assertEquals((short) 5, (short) person.getT());
    }

    @Test
    public void throwFromOnHandlerRunsAllHandlers() {
        SingleSubject<String> value1 = SingleSubject.create();
        SingleSubject<String> value2 = SingleSubject.create();

        try (TestLogger logger = new TestLogger()) {
            MockTransport mockTransport = new MockTransport();
            HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

            hubConnection.on("inc", (param1) -> {
                value1.onSuccess(param1);
                if (true) {
                    throw new RuntimeException("throw from on handler");
                }
            }, String.class);
            hubConnection.on("inc", (param1) -> {
                value2.onSuccess(param1);
            }, String.class);

            hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc\",\"arguments\":[\"Hello World\"]}" + RECORD_SEPARATOR);

            // Confirming that our handler was called and the correct message was passed in.
            assertEquals("Hello World", value1.timeout(30, TimeUnit.SECONDS).blockingGet());
            assertEquals("Hello World", value2.timeout(30, TimeUnit.SECONDS).blockingGet());

            hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

            ILoggingEvent log = logger.assertLog("Invoking client side method 'inc' failed:");
            assertEquals("throw from on handler", log.getThrowableProxy().getMessage());
        }
    }

    @Test
    public void receiveHandshakeResponseAndMessage() {
        CompletableSubject complete = CompletableSubject.create();
        AtomicReference<Double> value = new AtomicReference<Double>(0.0);
        MockTransport mockTransport = new MockTransport(false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.on("inc", () -> {
            value.getAndUpdate((val) -> val + 1);

            complete.onComplete();
        });

        SingleSubject<ByteBuffer> handshakeMessageTask = mockTransport.getNextSentMessage();
        // On start we're going to receive the handshake response and also an invocation in the same payload.
        hubConnection.start();
        ByteBuffer sentMessage = handshakeMessageTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        String expectedSentMessage  = "{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR;
        assertEquals(expectedSentMessage, TestUtils.byteBufferToString(mockTransport.getSentMessages()[0]));

        mockTransport.receiveMessage("{}" + RECORD_SEPARATOR + "{\"type\":1,\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        // Confirming that our handler was called and that the counter property was incremented.
        complete.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(Double.valueOf(1), value.get());
    }

    @Test
    public void onClosedCallbackRunsWhenStopIsCalled()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        hubConnection.onClosed((ex) -> {
            assertNull(value1.get());
            value1.set("Closed callback ran.");
        });
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertEquals(value1.get(), "Closed callback ran.");
    }

    @Test
    public void multipleOnClosedCallbacksRunWhenStopIsCalled()  {
        AtomicReference<String> value1 = new AtomicReference<>();
        AtomicReference<String> value2 = new AtomicReference<>();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

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
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

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
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        mockTransport.receiveMessage("{\"type\":7,\"error\": \"There was an error\"}" + RECORD_SEPARATOR);

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void callingStartOnStartedHubConnectionNoops()  {
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com");
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void callingStartOnStartingHubConnectionWaitsForOriginalStart()  {
        CompletableSubject startedAccessToken = CompletableSubject.create();
        CompletableSubject continueAccessToken = CompletableSubject.create();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(new MockTransport(true))
                .withHttpClient(new TestHttpClient())
                .withAccessTokenProvider(Single.defer(() -> {
                    startedAccessToken.onComplete();
                    continueAccessToken.timeout(30, TimeUnit.SECONDS).blockingAwait();
                    return Single.just("test");
                }).subscribeOn(Schedulers.newThread()))
                .shouldSkipNegotiate(true)
                .build();
        Completable start = hubConnection.start();
        startedAccessToken.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTING, hubConnection.getConnectionState());

        Completable start2 = hubConnection.start();
        continueAccessToken.onComplete();
        start.timeout(30, TimeUnit.SECONDS).blockingAwait();
        start2.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        mockTransport.receiveMessage("{\"type\":1,\"target\":\"Send\",\"arguments\":[]}" + RECORD_SEPARATOR);
        hubConnection.stop();
    }

    @Test
    public void negotiateSentOnStart() {
        TestHttpClient client = new TestHttpClient()
        .on("POST", (req) -> Single.just(new HttpResponse(404, "", TestUtils.emptyByteBuffer)));

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withHttpClient(client)
                .build();

        HttpRequestException exception = assertThrows(HttpRequestException.class, () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Unexpected status code returned from negotiate: 404 .", exception.getMessage());
        assertEquals(404, exception.getStatusCode());

        List<HttpRequest> sentRequests = client.getSentRequests();
        assertEquals(1, sentRequests.size());
        assertEquals("http://example.com/negotiate?negotiateVersion=1", sentRequests.get(0).getUrl());
    }

    @Test
    public void negotiateThatRedirectsForeverFailsAfter100Tries() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://example.com\"}"))));

        HubConnection hubConnection = HubConnectionBuilder
            .create("http://example.com")
            .withHttpClient(client)
            .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
            () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
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
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void SkippingNegotiateDoesNotNegotiate() {
        try (TestLogger logger = new TestLogger(WebSocketTransport.class.getName())) {
            AtomicBoolean negotiateCalled = new AtomicBoolean(false);
            TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                    (req) -> {
                        negotiateCalled.set(true);
                        return Single.just(new HttpResponse(200, "",
                            TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                    });

            HubConnection hubConnection = HubConnectionBuilder
                    .create("http://example")
                    .withTransport(TransportEnum.WEBSOCKETS)
                    .shouldSkipNegotiate(true)
                    .withHandshakeResponseTimeout(1)
                    .withHttpClient(client)
                    .build();

            assertThrows(RuntimeException.class, () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
            assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
            assertFalse(negotiateCalled.get());

            logger.assertLog("Starting Websocket connection.");
        }
    }

    @Test
    public void ThrowsIfSkipNegotiationSetAndTransportIsNotWebSockets() {
        AtomicBoolean negotiateCalled = new AtomicBoolean(false);
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> {
                    negotiateCalled.set(true);
                    return Single.just(new HttpResponse(200, "",
                        TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example")
                .shouldSkipNegotiate(true)
                .withHttpClient(client)
                .build();

        try {
            hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        } catch (Exception e) {
            assertEquals("Negotiation can only be skipped when using the WebSocket transport directly with '.withTransport(TransportEnum.WEBSOCKETS)' on the 'HubConnectionBuilder'.", e.getMessage());
        }
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertFalse(negotiateCalled.get());
    }

    @Test
    public void connectionIdIsAvailableAfterStart() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void connectionTokenAppearsInQSConnectionIdIsOnConnectionInstance() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                                "\"negotiateVersion\": 1," +
                                "\"connectionToken\":\"connection-token-value\"," +
                                "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());
        assertEquals("http://example.com?id=connection-token-value", transport.getUrl());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void connectionTokenIsIgnoredIfNegotiateVersionIsNotPresentInNegotiateResponse() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                        "\"connectionToken\":\"connection-token-value\"," +
                        "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());
        assertEquals("http://example.com?id=bVOiRPG8-6YiJ6d7ZcTOVQ", transport.getUrl());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void negotiateVersionIsNotAddedIfAlreadyPresent() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=42",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                        "\"connectionToken\":\"connection-token-value\"," +
                        "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com?negotiateVersion=42")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", hubConnection.getConnectionId());
        assertEquals("http://example.com?negotiateVersion=42&id=bVOiRPG8-6YiJ6d7ZcTOVQ", transport.getUrl());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        assertNull(hubConnection.getConnectionId());
    }

    @Test
    public void afterSuccessfulNegotiateConnectsWithWebsocketsTransport() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ByteBuffer[] sentMessages = transport.getSentMessages();
        assertEquals(1, sentMessages.length);
        assertEquals("{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR, TestUtils.byteBufferToString(sentMessages[0]));
    }

    @Test
    public void afterSuccessfulNegotiateConnectsWithLongPollingTransport() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ByteBuffer[] sentMessages = transport.getSentMessages();
        assertEquals(1, sentMessages.length);
        assertEquals("{\"protocol\":\"json\",\"version\":1}" + RECORD_SEPARATOR, TestUtils.byteBufferToString(sentMessages[0]));
    }

    @Test
    public void TransportAllUsesLongPollingWhenServerOnlySupportLongPolling() {
        AtomicInteger requestCount = new AtomicInteger(0);
        CompletableSubject close = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("POST",
                        (req) -> Single.just(new HttpResponse(200, "",
                        		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                        + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
                .on("GET", (req) -> {
                    if (requestCount.get() < 2) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
                    }
                    assertTrue(close.blockingAwait(5, TimeUnit.SECONDS));
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                })
                .on("DELETE", (req) -> {
                    close.onComplete();
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
                });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.ALL)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(hubConnection.getTransport() instanceof LongPollingTransport);
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void ClientThatSelectsWebsocketsThrowsWhenWebsocketsAreNotAvailable() {
        TestHttpClient client = new TestHttpClient().on("POST",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
                .on("GET", (req) -> {
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.WEBSOCKETS)
                .withHttpClient(client)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());

        assertEquals(exception.getMessage(), "There were no compatible transports on the server.");
    }

    @Test
    public void ClientThatSelectsLongPollingThrowsWhenLongPollingIsNotAvailable() {
        TestHttpClient client = new TestHttpClient().on("POST",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                        + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());

        assertEquals(exception.getMessage(), "There were no compatible transports on the server.");
    }

    @Test
    public void ConnectionRestartDoesNotResetUserTransportEnum() {
        AtomicInteger requestCount = new AtomicInteger(0);
        AtomicReference<CompletableSubject> blockGet = new AtomicReference<CompletableSubject>(CompletableSubject.create());
        TestHttpClient client = new TestHttpClient()
            .on("POST", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            })
            .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                        + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]},"
                        + "{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
            .on("GET", (req) -> {
                if (requestCount.incrementAndGet() >= 3) {
                    blockGet.get().timeout(30, TimeUnit.SECONDS).blockingAwait();
                }
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
            })
            .on("DELETE", (req) -> {
                blockGet.get().onComplete();
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(hubConnection.getTransport() instanceof LongPollingTransport);
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

        requestCount.set(0);
        blockGet.set(CompletableSubject.create());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(hubConnection.getTransport() instanceof LongPollingTransport);
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void LongPollingTransportAccessTokenProviderThrowsOnInitialPoll() {
        TestHttpClient client = new TestHttpClient()
            .on("POST", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            })
            .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
            .on("GET", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
            });

        AtomicInteger accessTokenCount = new AtomicInteger(0);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.defer(() -> {
                    if (accessTokenCount.getAndIncrement() < 1) {
                        return Single.just("");
                    }
                    return Single.error(new RuntimeException("Error from accessTokenProvider"));
                }))
                .build();

        try {
            hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
            assertTrue(false);
        } catch (RuntimeException ex) {
            assertEquals("Error from accessTokenProvider", ex.getMessage());
        }
    }

    @Test
    public void LongPollingTransportAccessTokenProviderThrowsAfterHandshakeClosesConnection() {
        AtomicInteger requestCount = new AtomicInteger(0);
        CompletableSubject blockGet = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
            .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
            .on("GET", (req) -> {
                if (requestCount.getAndIncrement() > 1) {
                    blockGet.blockingAwait();
                }
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
            })
            .on("POST", "http://example.com?id=bVOiRPG8-6YiJ6d7ZcTOVQ", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            });

        AtomicInteger accessTokenCount = new AtomicInteger(0);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.defer(() -> {
                    if (accessTokenCount.getAndIncrement() < 5) {
                        return Single.just("");
                    }
                    return Single.error(new RuntimeException("Error from accessTokenProvider"));
                }))
                .build();

        CompletableSubject closed = CompletableSubject.create();
        hubConnection.onClosed((e) -> {
            closed.onComplete();
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        blockGet.onComplete();

        closed.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void LongPollingTransportAccessTokenProviderThrowsDuringStop() {
        AtomicInteger requestCount = new AtomicInteger(0);
        CompletableSubject blockGet = CompletableSubject.create();
        CompletableSubject blockStop = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
            .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
            .on("GET", (req) -> {
                if (requestCount.getAndIncrement() > 1) {
                    blockStop.onComplete();
                    blockGet.blockingAwait();
                }
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
            })
            .on("POST", "http://example.com?id=bVOiRPG8-6YiJ6d7ZcTOVQ", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            });

        AtomicInteger accessTokenCount = new AtomicInteger(0);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.defer(() -> {
                    if (accessTokenCount.getAndIncrement() < 5) {
                        return Single.just("");
                    }
                    return Single.error(new RuntimeException("Error from accessTokenProvider"));
                }))
                .build();

        CompletableSubject closed = CompletableSubject.create();
        hubConnection.onClosed((e) -> {
            closed.onComplete();
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        blockStop.timeout(30, TimeUnit.SECONDS).blockingAwait();
        try {
            hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
            assertTrue(false);
        } catch (Exception ex) {
            assertEquals("Error from accessTokenProvider", ex.getMessage());
        }
        blockGet.onComplete();
        closed.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void stopWithoutObservingWithLongPollingTransportStops() {
        AtomicInteger requestCount = new AtomicInteger(0);
        CompletableSubject blockGet = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
            .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
            .on("GET", (req) -> {
                if (requestCount.getAndIncrement() > 1) {
                    blockGet.blockingAwait();
                }
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
            })
            .on("POST", "http://example.com?id=bVOiRPG8-6YiJ6d7ZcTOVQ", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            });

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransport(TransportEnum.LONG_POLLING)
                .withHttpClient(client)
                .build();

        CompletableSubject closed = CompletableSubject.create();
        hubConnection.onClosed((e) -> {
            closed.onComplete();
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        hubConnection.stop();
        closed.timeout(30, TimeUnit.SECONDS).blockingAwait();
        blockGet.onComplete();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionClosesAndRunsOnClosedCallbackAfterCloseMessageWithLongPolling()  {
        AtomicInteger requestCount = new AtomicInteger(0);
        CompletableSubject blockGet = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
            .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                        TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))))
            .on("GET", (req) -> {
                if (requestCount.getAndIncrement() > 1) {
                    blockGet.blockingAwait();
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"type\":7}" + RECORD_SEPARATOR)));
                }
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR)));
            })
            .on("POST", "http://example.com?id=bVOiRPG8-6YiJ6d7ZcTOVQ", (req) -> {
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("")));
            });

        HubConnection hubConnection = HubConnectionBuilder
            .create("http://example.com")
            .withTransport(TransportEnum.LONG_POLLING)
            .withHttpClient(client)
            .build();

        CompletableSubject closed = CompletableSubject.create();
        hubConnection.onClosed((ex) -> {
            closed.onComplete();
        });
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        blockGet.onComplete();

        closed.timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void receivingServerSentEventsTransportFromNegotiateFails() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"ServerSentEvents\",\"transferFormats\":[\"Text\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());

        assertEquals(exception.getMessage(), "There were no compatible transports on the server.");
    }

    @Test
    public void negotiateThatReturnsErrorThrowsFromStart() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"error\":\"Test error.\"}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withHttpClient(client)
                .withTransportImplementation(transport)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
            () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Test error.", exception.getMessage());
    }

    @Test
    public void DetectWhenTryingToConnectToClassicSignalRServer() {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"Url\":\"/signalr\"," +
                        "\"ConnectionToken\":\"X97dw3uxW4NPPggQsYVcNcyQcuz4w2\"," +
                        "\"ConnectionId\":\"05265228-1e2c-46c5-82a1-6a5bcc3f0143\"," +
                        "\"KeepAliveTimeout\":10.0," +
                        "\"DisconnectTimeout\":5.0," +
                        "\"TryWebSockets\":true," +
                        "\"ProtocolVersion\":\"1.5\"," +
                        "\"TransportConnectTimeout\":30.0," +
                        "\"LongPollDelay\":0.0}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withHttpClient(client)
                .withTransportImplementation(transport)
                .build();

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Detected an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.",
                exception.getMessage());
    }

    @Test
    public void negotiateRedirectIsFollowed()  {
        TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\"}"))))
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1",
                (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\",\"accessToken\":\"newToken\"}")));
                })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                    token.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.just("User Registered Token"))
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer User Registered Token", beforeRedirectToken.get());
        assertEquals("Bearer newToken", token.get());

        // Clear the tokens to see if they get reset to the proper values
        beforeRedirectToken.set("");
        token.set("");

        // Restart the connection to make sure that the original accessTokenProvider that we registered is still registered before the redirect.
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.just("secretToken"))
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("Bearer secretToken", token.get());
    }

    @Test
    public void AccessTokenProviderCanProvideDifferentValues() {
        AtomicReference<String> token = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            token.set(req.getHeaders().get("Authorization"));
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        AtomicInteger i = new AtomicInteger(0);
        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.defer(() -> Single.just("secret" + i.getAndIncrement())))
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer secret0", token.get());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer secret1", token.get());
    }

    @Test
    public void accessTokenProviderIsOverriddenFromRedirectNegotiate() {
        AtomicReference<String> token = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
            .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "",
            		TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\",\"accessToken\":\"newToken\"}"))))
            .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                token.set(req.getHeaders().get("Authorization"));
                return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\","
                        + "\"connectionToken\":\"connection-token-value\","
                        + "\"negotiateVersion\":1,"
                        + "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
            });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withAccessTokenProvider(Single.just("secretToken"))
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\",\"accessToken\":\"newToken\"}")));
                })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                    token.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\","
                            + "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer newToken", token.get());

        // Clear the tokens to see if they get reset to the proper values
        beforeRedirectToken.set("");
        token.set("");

        // Restart the connection to make sure that the original accessTokenProvider that we registered is still registered before the redirect.
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\",\"accessToken\":\"firstRedirectToken\"}")));
                    } else {
                        redirectToken.set(req.getHeaders().get("Authorization"));
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\",\"accessToken\":\"secondRedirectToken\"}")));
                    }
                })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> {
                    token.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                });

        MockTransport transport = new MockTransport(true);
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals("Bearer firstRedirectToken", token.get());

        // Clear the tokens to see if they get reset to the proper values
        redirectToken.set("");
        token.set("");

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertNull(redirectToken.get());
        assertEquals("Bearer secondRedirectToken", token.get());
    }

    @Test
    public void ErrorInAccessTokenProviderThrowsFromStart() {
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withAccessTokenProvider(Single.defer(() -> Single.error(new RuntimeException("Error from accessTokenProvider"))))
                .build();

        try {
            hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
            assertTrue(false);
        } catch (RuntimeException ex) {
            assertEquals("Error from accessTokenProvider", ex.getMessage());
        }
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        assertEquals("Server timeout elapsed without receiving a message from the server.", closedSubject.timeout(30, TimeUnit.SECONDS).blockingGet().getMessage());
    }

    @Test
    public void connectionSendsPingsRegularly() throws InterruptedException {
        MockTransport mockTransport = new MockTransport(true, false);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        hubConnection.setKeepAliveInterval(1);
        hubConnection.setTickRate(1);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        String message = TestUtils.byteBufferToString(mockTransport.getNextSentMessage().timeout(30, TimeUnit.SECONDS).blockingGet());
        assertEquals("{\"type\":6}" + RECORD_SEPARATOR, message);
        message = TestUtils.byteBufferToString(mockTransport.getNextSentMessage().timeout(30, TimeUnit.SECONDS).blockingGet());
        assertEquals("{\"type\":6}" + RECORD_SEPARATOR, message);

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
    }

    @Test
    public void userAgentHeaderIsSet() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("User-Agent"));
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("User-Agent", "Updated Value")
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("User-Agent", "")
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });


        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("ExampleHeader", "ExampleValue")
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("Authorization", "ExampleValue")
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop();
        assertEquals("ExampleValue", header.get());

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\",\"accessToken\":\"redirectToken\"}\"}")));
                        })
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            afterRedirectHeader.set(req.getHeaders().get("Authorization"));
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("Authorization", "ExampleValue")
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().blockingAwait();
        assertEquals("ExampleValue", beforeRedirectHeader.get());
        assertEquals("Bearer redirectToken", afterRedirectHeader.get());

        // Making sure you can do this after restarting the HubConnection.
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().blockingAwait();
        assertEquals("ExampleValue", beforeRedirectHeader.get());
        assertEquals("Bearer redirectToken", afterRedirectHeader.get());
    }

    @Test
    public void sameHeaderSetTwiceGetsOverwritten() {
        AtomicReference<String> header = new AtomicReference<>();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> {
                            header.set(req.getHeaders().get("ExampleHeader"));
                            return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                            + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                        });


        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder.create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .withHeader("ExampleHeader", "ExampleValue")
                .withHeader("ExampleHeader", "New Value")
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
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

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void hubConnectionCanBeStartedAfterBeingStoppedAndRedirected() {
        MockTransport mockTransport = new MockTransport();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\"}"))))
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(mockTransport)
                .withHttpClient(client)
                .build();

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
    }

    @Test
    public void non200FromNegotiateThrowsError() {
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1",
                        (req) -> Single.just(new HttpResponse(500, "Internal server error", TestUtils.emptyByteBuffer)));

        MockTransport transport = new MockTransport();
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(transport)
                .withHttpClient(client)
                .build();

                HttpRequestException exception = assertThrows(HttpRequestException.class,
            () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals("Unexpected status code returned from negotiate: 500 Internal server error.", exception.getMessage());
        assertEquals(500, exception.getStatusCode());
    }

    @Test
    public void hubConnectionCloseCallsStop() {
        MockTransport mockTransport = new MockTransport();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("{\"url\":\"http://testexample.com/\"}"))))
                .on("POST", "http://testexample.com/negotiate?negotiateVersion=1", (req) -> Single.just(new HttpResponse(200, "",
                		TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}"))));

        CompletableSubject close = CompletableSubject.create();

        try (HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withTransportImplementation(mockTransport)
                .withHttpClient(client)
                .build()) {

            hubConnection.onClosed(e -> {
                close.onComplete();
            });
            hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
            assertEquals(HubConnectionState.CONNECTED, hubConnection.getConnectionState());
        }

        assertTrue(close.blockingAwait(30, TimeUnit.SECONDS));
    }

    @Test
    public void hubConnectionStopDuringConnecting() {
        MockTransport mockTransport = new MockTransport();
        CompletableSubject waitForStop = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("POST", "http://example.com/negotiate?negotiateVersion=1", (req) ->
                {
                    return Single.defer(() -> {
                        waitForStop.blockingAwait();
                        return Single.just(new HttpResponse(200, "",
                            TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                    }).subscribeOn(Schedulers.computation());
                });

        CompletableSubject close = CompletableSubject.create();

        HubConnection hubConnection = HubConnectionBuilder
            .create("http://example.com")
            .withTransportImplementation(mockTransport)
            .withHttpClient(client)
            .build();

        hubConnection.onClosed(e -> {
            close.onComplete();
        });
        hubConnection.start();
        assertEquals(HubConnectionState.CONNECTING, hubConnection.getConnectionState());

        Completable stopTask = hubConnection.stop();
        waitForStop.onComplete();
        stopTask.timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

        assertTrue(close.blockingAwait(30, TimeUnit.SECONDS));
    }

    @Test
    public void serverTimeoutIsSetThroughBuilder()
    {
        long timeout = 60 * 1000;
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withServerTimeout(timeout)
                .build();

        assertEquals(timeout, hubConnection.getServerTimeout());
    }

    @Test
    public void keepAliveIntervalIsSetThroughBuilder()
    {
        long interval = 60 * 1000;
        HubConnection hubConnection = HubConnectionBuilder
                .create("http://example.com")
                .withKeepAliveInterval(interval)
                .build();

        assertEquals(interval, hubConnection.getKeepAliveInterval());
    }

    @Test
    public void WebsocketStopLoggedOnce() {
        try (TestLogger logger = new TestLogger(WebSocketTransport.class.getName())) {
            AtomicBoolean negotiateCalled = new AtomicBoolean(false);
            TestHttpClient client = new TestHttpClient().on("POST", "http://example.com/negotiate?negotiateVersion=1",
                    (req) -> {
                        negotiateCalled.set(true);
                        return Single.just(new HttpResponse(200, "",
                            TestUtils.stringToByteBuffer("{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\""
                                    + "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}")));
                    });

            HubConnection hubConnection = HubConnectionBuilder
                    .create("http://example")
                    .withTransport(TransportEnum.WEBSOCKETS)
                    .shouldSkipNegotiate(true)
                    .withHandshakeResponseTimeout(100)
                    .withHttpClient(client)
                    .build();

            Completable startTask = hubConnection.start().timeout(30, TimeUnit.SECONDS);
            hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

            assertThrows(RuntimeException.class, () -> startTask.blockingAwait());
            assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());
            assertFalse(negotiateCalled.get());

            ILoggingEvent[] logs = logger.getLogs();
            int count = 0;
            for (ILoggingEvent iLoggingEvent : logs) {
                if (iLoggingEvent.getFormattedMessage().startsWith("WebSocket connection stopped.")) {
                    count++;
                }
            }

            assertEquals(1, count);
        }
    }

    // https://github.com/dotnet/aspnetcore/issues/49043
    @Test
    public void sendsCloseMessageOnStop() throws InterruptedException {
        MockTransport mockTransport = new MockTransport(true, true);
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();

        hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

        ByteBuffer[] messages = mockTransport.getSentMessages();

        // handshake, close
        assertEquals(2, messages.length);
        String message = TestUtils.byteBufferToString(messages[1]);
        assertEquals("{\"type\":7,\"allowReconnect\":false}" + RECORD_SEPARATOR, message);
    }
}
