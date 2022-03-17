// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.nio.ByteBuffer;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;

import io.reactivex.rxjava3.core.Single;
import io.reactivex.rxjava3.subjects.CompletableSubject;

public class LongPollingTransportTest {

    @Test
    public void LongPollingFailsToConnectWith404Response() {
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> Single.just(new HttpResponse(404, "", TestUtils.emptyByteBuffer)));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        Throwable exception = assertThrows(RuntimeException.class, () -> transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals(Exception.class, exception.getCause().getClass());
        assertEquals("Failed to connect.", exception.getCause().getMessage());
        assertFalse(transport.isActive());
    }

    @Test
    public void LongPollingTransportCantSendBeforeStart() {
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> Single.just(new HttpResponse(404, "", TestUtils.emptyByteBuffer)));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        ByteBuffer sendBuffer = TestUtils.stringToByteBuffer("First");
        Throwable exception = assertThrows(RuntimeException.class, () -> transport.send(sendBuffer).timeout(30, TimeUnit.SECONDS).blockingAwait());
        assertEquals(Exception.class, exception.getCause().getClass());
        assertEquals("Cannot send unless the transport is active.", exception.getCause().getMessage());
        assertFalse(transport.isActive());
    }

    @Test
    public void StatusCode204StopsLongPollingTriggersOnClosed() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        CompletableSubject block = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        AtomicBoolean onClosedRan = new AtomicBoolean(false);
        transport.setOnClose((error) -> {
            onClosedRan.set(true);
            block.onComplete();
        });

        assertFalse(onClosedRan.get());
        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(block.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(onClosedRan.get());
        assertFalse(transport.isActive());
    }

    @Test
    public void LongPollingFailsWhenReceivingUnexpectedErrorCode() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        CompletableSubject blocker = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    return Single.just(new HttpResponse(999, "", TestUtils.emptyByteBuffer));
                })
                .on("DELETE", (req) -> Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer(""))));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        AtomicBoolean onClosedRan = new AtomicBoolean(false);
        transport.setOnClose((error) -> {
            onClosedRan.set(true);
            assertEquals("Unexpected response code 999.", error);
            blocker.onComplete();
        });

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(blocker.blockingAwait(30, TimeUnit.SECONDS));
        assertFalse(transport.isActive());
        assertTrue(onClosedRan.get());
    }

    @Test
    public void CanSetAndTriggerOnReceive() {
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer)));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        AtomicBoolean onReceivedRan = new AtomicBoolean(false);
        transport.setOnReceive((message) -> {
            onReceivedRan.set(true);
            assertEquals("TEST", TestUtils.byteBufferToString(message));
        });

        // The transport doesn't need to be active to trigger onReceive for the case
        // when we are handling the last outstanding poll.
        ByteBuffer onReceiveBuffer = TestUtils.stringToByteBuffer("TEST");
        transport.onReceive(onReceiveBuffer);
        assertTrue(onReceivedRan.get());
    }

    @Test
    public void LongPollingTransportOnReceiveGetsCalled() {
        AtomicInteger requestCount = new AtomicInteger();
        CompletableSubject block = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    } else if (requestCount.get() == 1) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("TEST")));
                    }

                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        AtomicBoolean onReceiveCalled = new AtomicBoolean(false);
        AtomicReference<String> message = new AtomicReference<>();
        transport.setOnReceive((msg -> {
            onReceiveCalled.set(true);
            message.set(TestUtils.byteBufferToString(msg));
            block.onComplete();
        }) );

        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(block.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(onReceiveCalled.get());
        assertEquals("TEST", message.get());
    }

    @Test
    public void LongPollingTransportOnReceiveGetsCalledMultipleTimes() {
        AtomicInteger requestCount = new AtomicInteger();
        CompletableSubject blocker = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    } else if (requestCount.get() == 1) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("FIRST")));
                    } else if (requestCount.get() == 2) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("SECOND")));
                    } else if (requestCount.get() == 3) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.stringToByteBuffer("THIRD")));
                    }

                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        AtomicBoolean onReceiveCalled = new AtomicBoolean(false);
        AtomicReference<String> message = new AtomicReference<>("");
        AtomicInteger messageCount = new AtomicInteger();
        transport.setOnReceive((msg) -> {
            onReceiveCalled.set(true);
            message.set(message.get() + TestUtils.byteBufferToString(msg));
            if (messageCount.incrementAndGet() == 3) {
                blocker.onComplete();
            }
        });

        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(blocker.blockingAwait(30, TimeUnit.SECONDS));
        assertTrue(onReceiveCalled.get());
        assertEquals("FIRSTSECONDTHIRD", message.get());
    }

    @Test
    public void LongPollingTransportSendsHeaders() {
        AtomicInteger requestCount = new AtomicInteger();
        AtomicReference<String> headerValue = new AtomicReference<>();
        CompletableSubject close = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    assertTrue(close.blockingAwait(30, TimeUnit.SECONDS));
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                }).on("POST", (req) -> {
                    assertFalse(req.getHeaders().isEmpty());
                    headerValue.set(req.getHeaders().get("KEY"));
                    return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        headers.put("KEY", "VALUE");
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        ByteBuffer sendBuffer = TestUtils.stringToByteBuffer("TEST");
        assertTrue(transport.send(sendBuffer).blockingAwait(30, TimeUnit.SECONDS));
        close.onComplete();
        assertEquals(headerValue.get(), "VALUE");
    }

    @Test
    public void LongPollingTransportSetsAuthorizationHeader() {
        AtomicInteger requestCount = new AtomicInteger();
        AtomicReference<String> headerValue = new AtomicReference<>();
        CompletableSubject close = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    assertTrue(close.blockingAwait(30, TimeUnit.SECONDS));
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                })
                .on("POST", (req) -> {
                    assertFalse(req.getHeaders().isEmpty());
                    headerValue.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        Single<String> tokenProvider = Single.just("TOKEN");
        LongPollingTransport transport = new LongPollingTransport(headers, client, tokenProvider);
        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        ByteBuffer sendBuffer = TestUtils.stringToByteBuffer("TEST");
        assertTrue(transport.send(sendBuffer).blockingAwait(30, TimeUnit.SECONDS));
        assertEquals(headerValue.get(), "Bearer TOKEN");
        close.onComplete();
    }

    @Test
    public void LongPollingTransportRunsAccessTokenProviderEveryRequest() {
        AtomicInteger requestCount = new AtomicInteger();
        AtomicReference<String> headerValue = new AtomicReference<>();
        CompletableSubject secondGet = CompletableSubject.create();
        CompletableSubject close = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    assertEquals("Bearer TOKEN1", req.getHeaders().get("Authorization"));
                    secondGet.onComplete();
                    assertTrue(close.blockingAwait(30, TimeUnit.SECONDS));
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                })
                .on("POST", (req) -> {
                    assertFalse(req.getHeaders().isEmpty());
                    headerValue.set(req.getHeaders().get("Authorization"));
                    return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                });

        AtomicInteger i = new AtomicInteger(0);
        Map<String, String> headers = new HashMap<>();
        Single<String> tokenProvider = Single.defer(() -> Single.just("TOKEN" + i.getAndIncrement()));
        LongPollingTransport transport = new LongPollingTransport(headers, client, tokenProvider);
        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        secondGet.blockingAwait(30, TimeUnit.SECONDS);
        ByteBuffer sendBuffer = TestUtils.stringToByteBuffer("TEST");
        assertTrue(transport.send(sendBuffer).blockingAwait(30, TimeUnit.SECONDS));
        assertEquals("Bearer TOKEN2", headerValue.get());
        close.onComplete();
    }

    @Test
    public void After204StopDoesNotTriggerOnCloseAgain() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        CompletableSubject block = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                })
                .on("DELETE", (req) -> {
                    return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        AtomicBoolean onClosedRan = new AtomicBoolean(false);
        AtomicInteger onCloseCount = new AtomicInteger(0);
        transport.setOnClose((error) -> {
            onClosedRan.set(true);
            onCloseCount.incrementAndGet();
            block.onComplete();
        });

        assertFalse(onClosedRan.get());
        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(block.blockingAwait(30, TimeUnit.SECONDS));
        assertEquals(1, onCloseCount.get());
        assertTrue(onClosedRan.get());
        assertFalse(transport.isActive());

        assertTrue(transport.stop().blockingAwait(30, TimeUnit.SECONDS));
        assertEquals(1, onCloseCount.get());
    }

    @Test
    public void StoppingTransportRunsCloseHandlersOnce() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        CompletableSubject block = CompletableSubject.create();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    } else {
                        assertTrue(block.blockingAwait(30, TimeUnit.SECONDS));
                        return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                    }
                })
                .on("DELETE", (req) ->{
                    //Unblock the last poll when we sent the DELETE request.
                    block.onComplete();
                    return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        AtomicInteger onCloseCount = new AtomicInteger(0);
        transport.setOnClose((error) -> {
            onCloseCount.incrementAndGet();
        });

        assertEquals(0, onCloseCount.get());
        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();
        assertTrue(transport.stop().blockingAwait(30, TimeUnit.SECONDS));
        assertEquals(1, onCloseCount.get());
        assertFalse(transport.isActive());
    }

    @Test
    public void ErrorFromClosePropagatesOnSecondStopCall() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", TestUtils.emptyByteBuffer));
                    }
                    return Single.just(new HttpResponse(204, "", TestUtils.emptyByteBuffer));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        transport.start("http://example.com").timeout(30, TimeUnit.SECONDS).blockingAwait();

        RuntimeException exception = assertThrows(RuntimeException.class, () -> transport.stop().blockingAwait(100, TimeUnit.SECONDS));
        assertEquals("Request has no handler: DELETE http://example.com", exception.getMessage());
    }
}
