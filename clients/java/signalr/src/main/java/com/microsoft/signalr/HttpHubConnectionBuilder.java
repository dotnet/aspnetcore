// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.time.Duration;
import java.util.HashMap;
import java.util.Map;

import io.reactivex.Single;

public class HttpHubConnectionBuilder {
    private final String url;
    private Transport transport;
    private HttpClient httpClient;
    private boolean skipNegotiate;
    private Single<String> accessTokenProvider;
    private Duration handshakeResponseTimeout;
    private Map<String, String> headers;

    HttpHubConnectionBuilder(String url) {
        this.url = url;
    }

    HttpHubConnectionBuilder withTransport(Transport transport) {
        this.transport = transport;
        return this;
    }

    HttpHubConnectionBuilder withHttpClient(HttpClient httpClient) {
        this.httpClient = httpClient;
        return this;
    }

    public HttpHubConnectionBuilder shouldSkipNegotiate(boolean skipNegotiate) {
        this.skipNegotiate = skipNegotiate;
        return this;
    }

    public HttpHubConnectionBuilder withAccessTokenProvider(Single<String> accessTokenProvider) {
        this.accessTokenProvider = accessTokenProvider;
        return this;
    }

    public HttpHubConnectionBuilder withHandshakeResponseTimeout(Duration timeout) {
        this.handshakeResponseTimeout = timeout;
        return this;
    }

    public HttpHubConnectionBuilder withHeaders(Map<String, String> headers) {
        this.headers = headers;
        return this;
    }

    public HttpHubConnectionBuilder withHeader(String name, String value) {
        if (headers == null) {
            this.headers = new HashMap<>();
        }
        this.headers.put(name, value);
        return this;
    }

    public HubConnection build() {
        return new HubConnection(url, transport, skipNegotiate, httpClient, accessTokenProvider, handshakeResponseTimeout, headers);
    }
}