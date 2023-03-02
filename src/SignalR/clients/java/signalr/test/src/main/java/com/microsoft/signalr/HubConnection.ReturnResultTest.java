// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.nio.ByteBuffer;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;

import io.reactivex.rxjava3.core.Single;
import io.reactivex.rxjava3.subjects.CompletableSubject;
import io.reactivex.rxjava3.subjects.SingleSubject;

@ExtendWith({RxJavaUnhandledExceptionsExtensions.class})
class HubConnectionReturnResultTest {
    private static final String RECORD_SEPARATOR = "\u001e";
    
    @Test
    public void returnFromOnHandlerNoParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicBoolean handlerCalled = new AtomicBoolean();

        hubConnection.onWithResult("inc", () -> {
            handlerCalled.set(true);
            return Single.just(10);
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
    public void missingReturningOnHandlerWithRequestedResult() {
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
    public void missingOnHandlerWithRequestedResult() {
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
    public void throwFromReturningOnHandlerWithRequestedResult() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.onWithResult("inc", () -> {
            boolean b = true;
            if (b) {
                throw new RuntimeException("Custom error.");
            }
            return Single.just("value");
        });

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"error\":\"Custom error.\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void cannotRegisterMultipleReturnHandlers() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.onWithResult("inc", () -> {
            return Single.just("value");
        });

        RuntimeException ex = assertThrows(RuntimeException.class, () -> {
            hubConnection.onWithResult("inc", () -> {
                return Single.just("value2");
            });
        });
        assertEquals("'inc' already has a value returning handler. Multiple return values are not supported.", ex.getMessage());
    }

    @Test
    public void logsWhenReturningResultButResultNotExpected() {
        try (TestLogger logger = new TestLogger()) {
            CompletableSubject complete = CompletableSubject.create();
            MockTransport mockTransport = new MockTransport();
            HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

            hubConnection.onWithResult("m", () -> {
                return Single.just(42);
            });

            hubConnection.on("fin", () -> {
                complete.onComplete();
            });

            hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"m\",\"arguments\":[]}" + RECORD_SEPARATOR);
            // send another invocation message and wait for it to be processed to make sure the first invocation was processed
            mockTransport.receiveMessage("{\"type\":1,\"target\":\"fin\",\"arguments\":[]}" + RECORD_SEPARATOR);

            complete.timeout(30, TimeUnit.SECONDS).blockingAwait();

            logger.assertLog("Result given for 'm' method but server is not expecting a result.");
        }
    }

    @Test
    public void returnFromOnHandlerOneParam() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i) -> {
            handlerCalled.set(i);
            return Single.just(10);
        }, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[1]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":10}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerTwoParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            return Single.just("bob");
        }, String.class, Integer.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[1,13]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerThreeParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();
        AtomicReference<Integer[]> handler3Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j, k) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            handler3Called.set(k);
            return Single.just("bob");
        }, String.class, Integer.class, Integer[].class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[1,13,[1,2,3]]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        assertArrayEquals(new Integer[] { 1, 2, 3 }, handler3Called.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerFourParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();
        AtomicReference<Integer[]> handler3Called = new AtomicReference<>();
        AtomicReference<Boolean> handler4Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j, k, l) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            handler3Called.set(k);
            handler4Called.set(l);
            return Single.just("bob");
        }, String.class, Integer.class, Integer[].class, Boolean.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"1\",13,[1,2,3],true]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        assertArrayEquals(new Integer[] { 1, 2, 3 }, handler3Called.get());
        assertEquals(true, handler4Called.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerFiveParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();
        AtomicReference<Integer[]> handler3Called = new AtomicReference<>();
        AtomicReference<Boolean> handler4Called = new AtomicReference<>();
        AtomicReference<String> handler5Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j, k, l, m) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            handler3Called.set(k);
            handler4Called.set(l);
            handler5Called.set(m);
            return Single.just("bob");
        }, String.class, Integer.class, Integer[].class, Boolean.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"1\",13,[1,2,3],true,\"t\"]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        assertArrayEquals(new Integer[] { 1, 2, 3 }, handler3Called.get());
        assertEquals(true, handler4Called.get());
        assertEquals("t", handler5Called.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerSixParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();
        AtomicReference<Integer[]> handler3Called = new AtomicReference<>();
        AtomicReference<Boolean> handler4Called = new AtomicReference<>();
        AtomicReference<String> handler5Called = new AtomicReference<>();
        AtomicReference<Double> handler6Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j, k, l, m, n) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            handler3Called.set(k);
            handler4Called.set(l);
            handler5Called.set(m);
            handler6Called.set(n);
            return Single.just("bob");
        }, String.class, Integer.class, Integer[].class, Boolean.class,
            String.class, Double.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"1\",13,[1,2,3],true,\"t\",1.5]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        assertArrayEquals(new Integer[] { 1, 2, 3 }, handler3Called.get());
        assertEquals(true, handler4Called.get());
        assertEquals("t", handler5Called.get());
        assertEquals(1.5, handler6Called.get().doubleValue());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerSevenParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();
        AtomicReference<Integer[]> handler3Called = new AtomicReference<>();
        AtomicReference<Boolean> handler4Called = new AtomicReference<>();
        AtomicReference<String> handler5Called = new AtomicReference<>();
        AtomicReference<Double> handler6Called = new AtomicReference<>();
        AtomicReference<String> handler7Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j, k, l, m, n, o) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            handler3Called.set(k);
            handler4Called.set(l);
            handler5Called.set(m);
            handler6Called.set(n);
            handler7Called.set(o);
            return Single.just("bob");
        }, String.class, Integer.class, Integer[].class, Boolean.class,
            String.class, Double.class, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"1\",13,[1,2,3],true,\"t\",1.5,\"h\"]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        assertArrayEquals(new Integer[] { 1, 2, 3 }, handler3Called.get());
        assertEquals(true, handler4Called.get());
        assertEquals("t", handler5Called.get());
        assertEquals(1.5, handler6Called.get().doubleValue());
        assertEquals("h", handler7Called.get());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void returnFromOnHandlerEightParams() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        AtomicReference<String> handlerCalled = new AtomicReference<>();
        AtomicReference<Integer> handler2Called = new AtomicReference<>();
        AtomicReference<Integer[]> handler3Called = new AtomicReference<>();
        AtomicReference<Boolean> handler4Called = new AtomicReference<>();
        AtomicReference<String> handler5Called = new AtomicReference<>();
        AtomicReference<Double> handler6Called = new AtomicReference<>();
        AtomicReference<String> handler7Called = new AtomicReference<>();
        AtomicReference<Integer> handler8Called = new AtomicReference<>();

        hubConnection.onWithResult("inc", (i, j, k, l, m, n, o, p) -> {
            handlerCalled.set(i);
            handler2Called.set(j);
            handler3Called.set(k);
            handler4Called.set(l);
            handler5Called.set(m);
            handler6Called.set(n);
            handler7Called.set(o);
            handler8Called.set(p);
            return Single.just("bob");
        }, String.class, Integer.class, Integer[].class, Boolean.class,
            String.class, Double.class, String.class, Integer.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"1\",13,[1,2,3],true,\"t\",1.5,\"h\",33]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        assertEquals("1", handlerCalled.get());
        assertEquals(13, handler2Called.get().intValue());
        assertArrayEquals(new Integer[] { 1, 2, 3 }, handler3Called.get());
        assertEquals(true, handler4Called.get());
        assertEquals("t", handler5Called.get());
        assertEquals(1.5, handler6Called.get().doubleValue());
        assertEquals("h", handler7Called.get());
        assertEquals(33, handler8Called.get().intValue());
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void clientResultHandlerDoesNotBlockOtherHandlers() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);
        CompletableSubject resultCalled = CompletableSubject.create();
        CompletableSubject completeResult = CompletableSubject.create();
        CompletableSubject nonResultCalled = CompletableSubject.create();

        hubConnection.onWithResult("inc", (i) -> {
            resultCalled.onComplete();
            completeResult.timeout(30, TimeUnit.SECONDS).blockingAwait();
            return Single.just("bob");
        }, String.class);

        hubConnection.on("inc2", (i) -> {
            nonResultCalled.onComplete();
        }, String.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"1\"]}" + RECORD_SEPARATOR);
        resultCalled.timeout(30, TimeUnit.SECONDS).blockingAwait();

        // Send an non-result invocation and make sure it's processed even with a blocking result invocation
        mockTransport.receiveMessage("{\"type\":1,\"target\":\"inc2\",\"arguments\":[\"1\"]}" + RECORD_SEPARATOR);
        nonResultCalled.timeout(30, TimeUnit.SECONDS).blockingAwait();

        completeResult.onComplete();

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"result\":\"bob\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }

    @Test
    public void clientResultReturnsErrorIfCannotParseArgument() {
        MockTransport mockTransport = new MockTransport();
        HubConnection hubConnection = TestUtils.createHubConnection("http://example.com", mockTransport);

        hubConnection.onWithResult("inc", (i) -> {
            return Single.just("bob");
        }, Integer.class);

        hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait();
        SingleSubject<ByteBuffer> sendTask = mockTransport.getNextSentMessage();
        mockTransport.receiveMessage("{\"type\":1,\"invocationId\":\"1\",\"target\":\"inc\",\"arguments\":[\"not int\"]}" + RECORD_SEPARATOR);

        ByteBuffer message = sendTask.timeout(30, TimeUnit.SECONDS).blockingGet();
        String expected = "{\"type\":3,\"invocationId\":\"1\",\"error\":\"Client failed to parse argument(s).\"}" + RECORD_SEPARATOR;
        assertEquals(expected, TestUtils.byteBufferToString(message));
    }
}
