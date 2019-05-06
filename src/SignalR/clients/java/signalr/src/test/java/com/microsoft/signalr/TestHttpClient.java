// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import io.reactivex.Single;

class TestHttpClient extends HttpClient {
    private TestHttpRequestHandler handler;
    private List<HttpRequest> sentRequests;

    public TestHttpClient() {
        this.sentRequests = new ArrayList<>();
        this.handler = (req) -> {
            return Single.error(new RuntimeException(String.format("Request has no handler: %s %s", req.getMethod(), req.getUrl())));
        };
    }

    @Override
    public Single<HttpResponse> send(HttpRequest request) {
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
        throw new RuntimeException("WebSockets isn't supported in testing currently.");
    }

    interface TestHttpRequestHandler {
        Single<HttpResponse> invoke(HttpRequest request);
    }
}