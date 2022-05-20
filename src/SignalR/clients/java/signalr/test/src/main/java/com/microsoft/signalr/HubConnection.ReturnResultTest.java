// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.nio.ByteBuffer;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;

import io.reactivex.rxjava3.subjects.SingleSubject;

@ExtendWith({RxJavaUnhandledExceptionsExtensions.class})
class HubConnectionReturnResultTest {
    private static final String RECORD_SEPARATOR = "\u001e";
    
    @Test
    public void returnFromOnHandler()  {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicBoolean handlerCalled = new AtomicBoolean();

        hubConnection.on("inc", () -> {
            handlerCalled.set(true);
            return 10;
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertTrue(handlerCalled.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":10}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void missingOnHandlerWithRequestedResult()  {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicBoolean handlerCalled = new AtomicBoolean();

        hubConnection.on("inc", () -> {
            handlerCalled.set(true);
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertTrue(handlerCalled.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"error\":\"Client did not provide a result.\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void missingReturningOnHandlerWithRequestedResult()  {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"error\":\"Client did not provide a result.\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void throwFromReturningOnHandlerWithRequestedResult()  {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicBoolean handlerCalled = new AtomicBoolean();

        hubConnection.on("inc", () -> {
            handlerCalled.set(true);

            boolean b = true;
            if (b) {
                throw new RuntimeException("Custom error.");
            }
            return "value";
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"error\":\"Custom error.\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }
}
