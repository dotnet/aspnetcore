// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import io.reactivex.rxjava3.core.Single;

class TestHttpClient extends HttpClient {
    private TestHttpRequestHandler handler;
    private List<HttpRequest> sentRequests;
    private boolean closeCalled;

    public TestHttpClient() {
        this.sentRequests = new ArrayList<>();
        this.handler = (req) -> {
            return Single.error(new RuntimeException(String.format("Request has no handler: %s %s", req.getMethod(), req.getUrl())));
        };
    }

    @Override
    public Single<HttpResponse> send(HttpRequest request) {
        return send(request, null);
    }

    @Override
    public Single<HttpResponse> send(HttpRequest request, ByteBuffer body) {
        this.sentRequests.add(request);
        return this.handler.invoke(request);
    }

    public List<HttpRequest> getSentRequests() {
        return sentRequests;
    }

    public TestHttpClient on(TestHttpRequestHandler handler) {
        this.handler = (req) -> handler.invoke(req);
        return this;
    }

    public TestHttpClient on(String method, TestHttpRequestHandler handler) {
        TestHttpRequestHandler oldHandler = this.handler;
        this.handler = (req) -> {
            if (req.getMethod().equals(method)) {
                return handler.invoke(req);
            }

            return oldHandler.invoke(req);
        };

        return this;
    }

    public TestHttpClient on(String method, String url, TestHttpRequestHandler handler) {
        TestHttpRequestHandler oldHandler = this.handler;
        this.handler = (req) -> {
            if (req.getMethod().equals(method) && req.getUrl().equals(url)) {
                return handler.invoke(req);
            }

            return oldHandler.invoke(req);
        };

        return this;
    }

    @Override
    public WebSocketWrapper createWebSocket(String url, Map<String, String> headers) {
        return new TestWebSocketWrapper(url, headers);
    }

    @Override
    public HttpClient cloneWithTimeOut(int timeoutInMilliseconds) {
        return this;
    }

    @Override
    public void close() {
        this.closeCalled = true;
    }

    public boolean getCloseCalled() {
        return this.closeCalled;
    }

    interface TestHttpRequestHandler {
        Single<HttpResponse> invoke(HttpRequest request);
    }
}
