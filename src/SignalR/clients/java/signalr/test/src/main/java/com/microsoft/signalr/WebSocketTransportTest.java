// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.nio.ByteBuffer;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

import org.junit.jupiter.api.Test;

import io.reactivex.rxjava3.core.Completable;
import io.reactivex.rxjava3.core.Single;

class WebSocketTransportTest {
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
        public Single<HttpResponse> send(HttpRequest request, ByteBuffer body) {
            return null;
        }

        @Override
        public WebSocketWrapper createWebSocket(String url, Map<String, String> headers) {
            return new TestWrapper();
        }

        @Override
        public HttpClient cloneWithTimeOut(int timeoutInMilliseconds) {
            return null;
        }

        @Override
        public void close() {
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
        public Completable send(ByteBuffer message) {
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
