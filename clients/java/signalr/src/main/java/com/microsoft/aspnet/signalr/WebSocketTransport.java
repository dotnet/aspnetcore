// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.util.Map;
import java.util.concurrent.CompletableFuture;

class WebSocketTransport implements Transport {
    private WebSocketWrapper webSocketClient;
    private OnReceiveCallBack onReceiveCallBack;
    private String url;
    private Logger logger;
    private HttpClient client;
    private Map<String, String> headers;

    private static final String HTTP = "http";
    private static final String HTTPS = "https";
    private static final String WS = "ws";
    private static final String WSS = "wss";

    public WebSocketTransport(Map<String, String> headers, HttpClient client, Logger logger) {
        this.logger = logger;
        this.client = client;
        this.headers = headers;
    }

    String getUrl() {
        return url;
    }

    private String formatUrl(String url) {
        if (url.startsWith(HTTPS)) {
            url = WSS + url.substring(HTTPS.length());
        } else if (url.startsWith(HTTP)) {
            url = WS + url.substring(HTTP.length());
        }

        return url;
    }

    @Override
    public CompletableFuture<Void> start(String url) {
        this.url = formatUrl(url);
        logger.log(LogLevel.Debug, "Starting Websocket connection.");
        this.webSocketClient = client.createWebSocket(this.url, this.headers);
        this.webSocketClient.setOnReceive((message) -> onReceive(message));
        this.webSocketClient.setOnClose((code, reason) -> onClose(code, reason));
        return webSocketClient.start().thenRun(() -> logger.log(LogLevel.Information, "WebSocket transport connected to: %s.", this.url));
    }

    @Override
    public CompletableFuture<Void> send(String message) {
        return webSocketClient.send(message);
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
        logger.log(LogLevel.Debug, "OnReceived callback has been set.");
    }

    @Override
    public void onReceive(String message) throws Exception {
        this.onReceiveCallBack.invoke(message);
    }

    @Override
    public CompletableFuture<Void> stop() {
        return webSocketClient.stop().whenComplete((i, j) -> logger.log(LogLevel.Information, "WebSocket connection stopped."));
    }

    void onClose(int code, String reason) {
        logger.log(LogLevel.Information, "WebSocket connection stopping with " +
                "code %d and reason '%s'.", code, reason);
    }
}
