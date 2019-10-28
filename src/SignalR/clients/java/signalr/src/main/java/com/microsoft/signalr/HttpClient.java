// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Map;

import io.reactivex.Single;

public abstract class HttpClient {
    public Single<HttpResponse> get(String url) {
        HttpRequest request = new HttpRequest();
        request.setUrl(url);
        request.setMethod("GET");
        return this.send(request);
    }

    public Single<HttpResponse> get(String url, HttpRequest options) {
        options.setUrl(url);
        options.setMethod("GET");
        return this.send(options);
    }

    public Single<HttpResponse> post(String url) {
        HttpRequest request = new HttpRequest();
        request.setUrl(url);
        request.setMethod("POST");
        return this.send(request);
    }

    public Single<HttpResponse> post(String url, String body, HttpRequest options) {
        options.setUrl(url);
        options.setMethod("POST");
        return this.send(options, body);
    }

    public Single<HttpResponse> post(String url, HttpRequest options) {
        options.setUrl(url);
        options.setMethod("POST");
        return this.send(options);
    }

    public Single<HttpResponse> delete(String url) {
        HttpRequest request = new HttpRequest();
        request.setUrl(url);
        request.setMethod("DELETE");
        return this.send(request);
    }

    public Single<HttpResponse> delete(String url, HttpRequest options) {
        options.setUrl(url);
        options.setMethod("DELETE");
        return this.send(options);
    }

    public abstract Single<HttpResponse> send(HttpRequest request);

    public abstract Single<HttpResponse> send(HttpRequest request, String body);

    public abstract WebSocketWrapper createWebSocket(String url, Map<String, String> headers);

    public abstract HttpClient cloneWithTimeOut(int timeoutInMilliseconds);
}
