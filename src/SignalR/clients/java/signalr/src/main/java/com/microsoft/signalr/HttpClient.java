// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.HashMap;
import java.util.Map;

import io.reactivex.Single;

class HttpRequest {
    private String method;
    private String url;
    private final Map<String, String> headers = new HashMap<>();

    public void setMethod(String method) {
        this.method = method;
    }

    public void setUrl(String url) {
        this.url = url;
    }

    public void addHeader(String key, String value) {
        this.headers.put(key, value);
    }

    public void addHeaders(Map<String, String> headers) {
        this.headers.putAll(headers);
    }

    public String getMethod() {
        return method;
    }

    public String getUrl() {
        return url;
    }

    public Map<String, String> getHeaders() {
        return headers;
    }
}

class HttpResponse {
    private final int statusCode;
    private final String statusText;
    private final String content;

    public HttpResponse(int statusCode) {
        this(statusCode, "");
    }

    public HttpResponse(int statusCode, String statusText) {
        this(statusCode, statusText, "");
    }

    public HttpResponse(int statusCode, String statusText, String content) {
        this.statusCode = statusCode;
        this.statusText = statusText;
        this.content = content;
    }

    public String getContent() {
        return content;
    }

    public int getStatusCode() {
        return statusCode;
    }

    public String getStatusText() {
        return statusText;
    }
}

abstract class HttpClient {
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

    public abstract WebSocketWrapper createWebSocket(String url, Map<String, String> headers);
}