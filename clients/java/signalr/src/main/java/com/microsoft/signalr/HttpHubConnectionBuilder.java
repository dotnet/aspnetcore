// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.concurrent.CompletableFuture;
import java.util.function.Supplier;

public class HttpHubConnectionBuilder {
    private String url;
    private Transport transport;
    private Logger logger;
    private HttpClient httpClient;
    private boolean skipNegotiate;
    private Supplier<CompletableFuture<String>> accessTokenProvider;

    HttpHubConnectionBuilder(String url) {
        this.url = url;
    }

    public HttpHubConnectionBuilder withTransport(Transport transport) {
        this.transport = transport;
        return this;
    }


    public HttpHubConnectionBuilder withHttpClient(HttpClient httpClient) {
        this.httpClient = httpClient;
        return this;
    }

    public HttpHubConnectionBuilder configureLogging(LogLevel logLevel) {
        this.logger = new ConsoleLogger(logLevel);
        return this;
    }

    public HttpHubConnectionBuilder shouldSkipNegotiate(boolean skipNegotiate) {
        this.skipNegotiate = skipNegotiate;
        return this;
    }

    public HttpHubConnectionBuilder withAccessTokenProvider(Supplier<CompletableFuture<String>> accessTokenProvider) {
        this.accessTokenProvider = accessTokenProvider;
        return this;
    }

    public HttpHubConnectionBuilder configureLogging(Logger logger) {
        this.logger = logger;
        return this;
    }

    public HttpHubConnectionBuilder withLogger(Logger logger) {
        this.logger = logger;
        return this;
    }

    public HubConnection build() {
        return new HubConnection(url, transport, skipNegotiate, logger, httpClient, accessTokenProvider);
    }
}