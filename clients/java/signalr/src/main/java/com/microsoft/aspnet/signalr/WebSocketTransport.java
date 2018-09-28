// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.net.URI;
import java.net.URISyntaxException;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

import okhttp3.*;

class WebSocketTransport implements Transport {
    private WebSocket websocketClient;
    private SignalRWebSocketListener webSocketListener;
    private OnReceiveCallBack onReceiveCallBack;
    private URI url;
    private Logger logger;
    private Map<String, String> headers;
    private OkHttpClient httpClient;
    private CompletableFuture<Void> startFuture = new CompletableFuture<>();

    private static final String HTTP = "http";
    private static final String HTTPS = "https";
    private static final String WS = "ws";
    private static final String WSS = "wss";

    public WebSocketTransport(String url, Logger logger, Map<String, String> headers) throws URISyntaxException {
        this.url = formatUrl(url);
        this.logger = logger;
        this.headers = headers;
        this.httpClient = new OkHttpClient();
    }

    public WebSocketTransport(String url, Logger logger, Map<String, String> headers, OkHttpClient httpClient) throws URISyntaxException {
        this.url = formatUrl(url);
        this.logger = logger;
        this.headers = headers;
        this.httpClient = httpClient;
    }

    public URI getUrl() {
        return url;
    }

    private URI formatUrl(String url) throws URISyntaxException {
        if (url.startsWith(HTTPS)) {
            url = WSS + url.substring(HTTPS.length());
        } else if (url.startsWith(HTTP)) {
            url = WS + url.substring(HTTP.length());
        }

        return new URI(url);
    }

    @Override
    public CompletableFuture start() {
            logger.log(LogLevel.Debug, "Starting Websocket connection.");
            webSocketListener = new SignalRWebSocketListener();
            websocketClient = createUpdatedWebSocket(webSocketListener);
            return startFuture;
    }

    @Override
    public CompletableFuture send(String message) {
        return CompletableFuture.runAsync(() -> websocketClient.send(message));
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
        logger.log(LogLevel.Debug, "OnReceived callback has been set");
    }

    @Override
    public void onReceive(String message) throws Exception {
        this.onReceiveCallBack.invoke(message);
    }

    @Override
    public CompletableFuture stop() {
        return CompletableFuture.runAsync(() -> {
            websocketClient.close(1000, "HubConnection stopped.");
            logger.log(LogLevel.Information, "WebSocket connection stopped");
        });
    }

    private WebSocket createUpdatedWebSocket(WebSocketListener webSocketListener) {
        Headers.Builder headerBuilder = new Headers.Builder();
        for (String key: headers.keySet()) {
            headerBuilder.add(key, headers.get(key));
        }
        Request request = new Request.Builder().url(url.toString())
                .headers(headerBuilder.build())
                .build();

        return this.httpClient.newWebSocket(request, webSocketListener);
    }


    private class SignalRWebSocketListener extends WebSocketListener {
        @Override
        public void onOpen(WebSocket webSocket, Response response) {
            startFuture.complete(null);
            logger.log(LogLevel.Information, "WebSocket transport connected to: %s", websocketClient.request().url());
        }

        @Override
        public void onMessage(WebSocket webSocket, String message) {
            try {
                onReceive(message);
            } catch (Exception e) {
                e.printStackTrace();
            }
        }

        @Override
        public void onClosing(WebSocket webSocket, int code, String reason) {
            logger.log(LogLevel.Information, "WebSocket connection stopping with " +
                    "code %d and reason %s", code, reason);
            // If the start future hasn't completed yet, then we need to complete it exceptionally.
            checkStartFailure();
        }

        @Override
        public void onFailure(WebSocket webSocket, Throwable t, Response response) {
            logger.log(LogLevel.Error, "Error : %d", t.getMessage());
            // If the start future hasn't completed yet, then we need to complete it exceptionally.
            checkStartFailure();
        }
    }

    private void checkStartFailure() {
        // If the start future hasn't completed yet, then we need to complete it exceptionally.
        if (!startFuture.isDone()) {
            String errorMessage = "There was an error starting the Websockets transport.";
            logger.log(LogLevel.Debug, errorMessage);
            startFuture.completeExceptionally(new RuntimeException(errorMessage));
        }
    }
}
