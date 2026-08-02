// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.util.HashMap;
import java.util.Map;

import io.reactivex.rxjava3.core.Single;
import okhttp3.OkHttpClient;

/**
 * A builder for configuring {@link HubConnection} instances.
 */
public class HttpHubConnectionBuilder {
    private final String url;
    private Transport transport;
    private HttpClient httpClient;
    private HubProtocol protocol = new GsonHubProtocol();
    private boolean skipNegotiate;
    private Single<String> accessTokenProvider;
    private long handshakeResponseTimeout = 0;
    private Map<String, String> headers;
    private TransportEnum transportEnum;
    private Action1<OkHttpClient.Builder> configureBuilder;
    private long serverTimeout = HubConnection.DEFAULT_SERVER_TIMEOUT;
    private long keepAliveInterval = HubConnection.DEFAULT_KEEP_ALIVE_INTERVAL;

    HttpHubConnectionBuilder(String url) {
        this.url = url;
    }

    //For testing purposes. The Transport interface isn't public.
    HttpHubConnectionBuilder withTransportImplementation(Transport transport) {
        this.transport = transport;
        return this;
    }

    /**
     * Sets the transport type to indicate which transport to be used by the {@link HubConnection}.
     *
     * @param transportEnum The type of transport to be used.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withTransport(TransportEnum transportEnum) {
        this.transportEnum = transportEnum;
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
     * Sets the {@link HubProtocol} to be used by the {@link HubConnection}.
     *
     * @param protocol The {@link HubProtocol} to be used by the {@link HubConnection}.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withHubProtocol(HubProtocol protocol) {
        this.protocol = protocol;
        return this;
    }

    /**
     * Indicates to the {@link HubConnection} that it should skip the negotiate process.
     * Note: This option only works with the {@link TransportEnum#WEBSOCKETS} transport selected via {@link #withTransport(TransportEnum) withTransport},
     * additionally the Azure SignalR Service requires the negotiate step so this will fail when using the Azure SignalR Service.
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
     * Sets a method that will be called when constructing the HttpClient to allow customization such as certificate validation, proxies, and cookies.
     * By default the client will have a cookie jar added and a read timeout for LongPolling.
     *
     * @param configureBuilder Callback for configuring the OkHttpClient.Builder.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder setHttpClientBuilderCallback(Action1<OkHttpClient.Builder> configureBuilder) {
        this.configureBuilder = configureBuilder;
        return this;
    }

    /**
     * Sets serverTimeout for the {@link HubConnection}.
     *
     * @param timeoutInMilliseconds The serverTimeout to be set.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withServerTimeout(long timeoutInMilliseconds) {
        this.serverTimeout = timeoutInMilliseconds;
        return this;
    }

    /**
     * Sets keepAliveInterval for the {@link HubConnection}.
     *
     * @param intervalInMilliseconds The keepAliveInterval to be set.
     * @return This instance of the HttpHubConnectionBuilder.
     */
    public HttpHubConnectionBuilder withKeepAliveInterval(long intervalInMilliseconds) {
        this.keepAliveInterval = intervalInMilliseconds;
        return this;
    }

    /**
     * Builds a new instance of {@link HubConnection}.
     *
     * @return A new instance of {@link HubConnection}.
     */
    public HubConnection build() {
        return new HubConnection(url, transport, skipNegotiate, httpClient, protocol, accessTokenProvider,
            handshakeResponseTimeout, headers, transportEnum, configureBuilder, serverTimeout, keepAliveInterval);
    }
}
