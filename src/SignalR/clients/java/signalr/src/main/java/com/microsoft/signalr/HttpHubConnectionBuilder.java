// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.HashMap;
import java.util.Map;

import io.reactivex.Single;

/**
 * A builder for configuring {@link HubConnection} instances.
 */
public class HttpHubConnectionBuilder {
    private final String url;
    private Transport transport;
    private HttpClient httpClient;
    private boolean skipNegotiate;
    private Single<String> accessTokenProvider;
    private long handshakeResponseTimeout = 0;
    private Map<String, String> headers;

    HttpHubConnectionBuilder(String url) {
        this.url = url;
    }

    /**
     * Sets the transport to be used by the {@link HubConnection}.
     *
     * @param transport The transport to be used.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    HttpHubConnectionBuilder withTransport(Transport transport) {
        this.transport = transport;
        return this;
    }

    /**
     * Sets the {@link HttpClient} to be used by the {@link HubConnection}.
     *
     * @param httpClient The {@link HttpClient} to be used by the {@link HubConnection}.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    HttpHubConnectionBuilder withHttpClient(HttpClient httpClient) {
        this.httpClient = httpClient;
        return this;
    }

    /**
     * Indicates to the {@link HubConnection} that it should skip the negotiate process.
     * Note: This option only works with the Websockets transport and the Azure SignalR Service require the negotiate step.
     *
     * @param skipNegotiate Boolean indicating if the {@link HubConnection} should skip the negotiate step.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder shouldSkipNegotiate(boolean skipNegotiate) {
        this.skipNegotiate = skipNegotiate;
        return this;
    }

    /**
     * Sets the access token provider for the {@link HubConnection}.
     *
     * @param accessTokenProvider The access token provider to be used by the {@link HubConnection}.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withAccessTokenProvider(Single<String> accessTokenProvider) {
        this.accessTokenProvider = accessTokenProvider;
        return this;
    }

    /**
     * Sets the duration the {@link HubConnection} should wait for a Handshake Response from the server.
     *
     * @param timeoutInMilliseconds The duration (specified in milliseconds) that the {@link HubConnection} should wait for a Handshake Response from the server.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withHandshakeResponseTimeout(long timeoutInMilliseconds) {
        this.handshakeResponseTimeout = timeoutInMilliseconds;
        return this;
    }

    /**
     * Sets a collection of Headers for the {@link HubConnection} to send with every Http request.
     *
     * @param headers A Map representing the collection of Headers that the {@link HubConnection} should send.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withHeaders(Map<String, String> headers) {
        this.headers = headers;
        return this;
    }

    /**
     * Sets a single header for the {@link HubConnection} to send.
     *
     * @param name The name of the header to set.
     * @param value The value of the header to be set.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withHeader(String name, String value) {
        if (headers == null) {
            this.headers = new HashMap<>();
        }
        this.headers.put(name, value);
        return this;
    }

    /**
     * Builds a new instance of {@link HubConnection}.
     *
     * @return A new instance of {@link HubConnection}.
     */
    public HubConnection build() {
        return new HubConnection(url, transport, skipNegotiate, httpClient, accessTokenProvider, handshakeResponseTimeout, headers);
    }
}
