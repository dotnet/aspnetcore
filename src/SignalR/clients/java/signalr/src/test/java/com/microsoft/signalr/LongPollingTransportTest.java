// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Test;

import io.reactivex.Single;

public class LongPollingTransportTest {

    @Test
    public void LongPollingFailsToConnectWith404Response() {
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> Single.just(new HttpResponse(404, "", "")));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        Throwable exception = assertThrows(RuntimeException.class, () -> transport.start("http://example.com").timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals(Exception.class, exception.getCause().getClass());
        assertEquals("Failed to connect.", exception.getCause().getMessage());
        assertFalse(transport.isActive());
    }

    @Test
    public void LongPollingTransportCantSendBeforeStart() {
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> Single.just(new HttpResponse(404, "", "")));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        Throwable exception = assertThrows(RuntimeException.class, () -> transport.send("First").timeout(1, TimeUnit.SECONDS).blockingAwait());
        assertEquals(Exception.class, exception.getCause().getClass());
        assertEquals("Cannot send unless the transport is active.", exception.getCause().getMessage());
        assertFalse(transport.isActive());
    }

    @Test
    public void StatusCode204StopsLongPolling() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", ""));
                    }
                    return Single.just(new HttpResponse(204, "", ""));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        transport.start("http://example.com").timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertFalse(transport.isActive());
    }

    @Test
    public void StatusCode204StopsLongPollingTriggersOnClosed() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", ""));
                    }
                    return Single.just(new HttpResponse(204, "", ""));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        AtomicBoolean onClosedRan = new AtomicBoolean(false);
        transport.setOnClose((error) -> {
            onClosedRan.set(true);
        });

        assertFalse(onClosedRan.get());
        transport.start("http://example.com").timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertTrue(onClosedRan.get());
        assertFalse(transport.isActive());
    }

    @Test
    public void LongPollingFailsWhenReceivingUnexpectedErrorCode() {
        AtomicBoolean firstPoll = new AtomicBoolean(true);
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (firstPoll.get()) {
                        firstPoll.set(false);
                        return Single.just(new HttpResponse(200, "", ""));
                    }
                    return Single.just(new HttpResponse(999, "", ""));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));
        AtomicBoolean onClosedRan = new AtomicBoolean(false);
        transport.setOnClose((error) -> {
            onClosedRan.set(true);
            assertEquals("Unexpected response code 999", error);
        });

        transport.start("http://example.com").timeout(1, TimeUnit.SECONDS).blockingAwait();
        assertFalse(transport.isActive());
        assertTrue(onClosedRan.get());
    }

    @Test
    public void CanSetAndTriggerOnReceive() {
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> Single.just(new HttpResponse(200, "", "")));

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        AtomicBoolean onReceivedRan = new AtomicBoolean(false);
        transport.setOnReceive((message) -> {
            onReceivedRan.set(true);
            assertEquals("TEST", message);
        });

        // The transport doesn't need to be active to trigger onReceive for the case
        // when we are handling the last outstanding poll.
        transport.onReceive("TEST");
        assertTrue(onReceivedRan.get());
    }

    @Test
    public void LongPollingTransportOnReceiveGetsCalled() {
        AtomicInteger requestCount = new AtomicInteger();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", ""));
                    } else if (requestCount.get() == 1){
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", "TEST"));
                    }

                    return Single.just(new HttpResponse(204, "", ""));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        AtomicBoolean onReceiveCalled = new AtomicBoolean(false);
        AtomicReference<String> message = new AtomicReference<>();
        transport.setOnReceive((msg -> {
            onReceiveCalled.set(true);
            message.set(msg);
        }) );

        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertTrue(onReceiveCalled.get());
        assertEquals("TEST", message.get());
    }

    @Test
    public void LongPollingTransportOnReceiveGetsCalledMultipleTimes() {
        AtomicInteger requestCount = new AtomicInteger();
        TestHttpClient client = new TestHttpClient()
                .on("GET", (req) -> {
                    if (requestCount.get() == 0) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", ""));
                    } else if (requestCount.get() == 1){
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", "FIRST"));
                    } else if (requestCount.get() == 2) {
                        requestCount.incrementAndGet();
                        return Single.just(new HttpResponse(200, "", "SECOND"));
                    }

                    return Single.just(new HttpResponse(204, "", ""));
                });

        Map<String, String> headers = new HashMap<>();
        LongPollingTransport transport = new LongPollingTransport(headers, client, Single.just(""));

        AtomicBoolean onReceiveCalled = new AtomicBoolean(false);
        AtomicReference<String> message = new AtomicReference<>("");
        transport.setOnReceive((msg -> {
            onReceiveCalled.set(true);
            message.set(message.get() + msg);
        }) );

        transport.setOnClose((error) -> {});

        transport.start("http://example.com").timeout(1, TimeUnit.SECONDS).blockingAwait();

        assertTrue(onReceiveCalled.get());
        assertEquals("FIRSTSECOND", message.get());
    }
}
