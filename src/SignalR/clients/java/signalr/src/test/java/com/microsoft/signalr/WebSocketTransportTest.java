// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

import org.junit.jupiter.api.Test;

import io.reactivex.Completable;
import io.reactivex.Single;

class WebSocketTransportTest {
    // @Test Skipping until we add functional test support
    public void WebSocketThrowsIfItCantConnect() {
        Transport transport = new WebSocketTransport(new HashMap<>(), new DefaultHttpClient());
        RuntimeException exception = assertThrows(RuntimeException.class, () -> transport.start("http://url.fake.example").blockingAwait(1, TimeUnit.SECONDS));
        assertEquals("There was an error starting the WebSocket transport.", exception.getMessage());
    }

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

    class WebSocketTestHttpClient extends HttpClient {
        @Override
        public Single<HttpResponse> send(HttpRequest request) {
            return null;
        }

        @Override
        public WebSocketWrapper createWebSocket(String url, Map<String, String> headers) {
            return new TestWrapper();
        }
    }

    class TestWrapper extends WebSocketWrapper {
        private WebSocketOnClosedCallback onClose;

        @Override
        public Completable start() {
            return Completable.complete();
        }

        @Override
        public Completable stop() {
            if (onClose != null) {
                onClose.invoke(null, "");
            }
            return Completable.complete();
        }

        @Override
        public Completable send(String message) {
            return null;
        }

        @Override
        public void setOnReceive(OnReceiveCallBack onReceive) {
        }

        @Override
        public void setOnClose(WebSocketOnClosedCallback onClose) {
            this.onClose = onClose;
        }
    }
}
