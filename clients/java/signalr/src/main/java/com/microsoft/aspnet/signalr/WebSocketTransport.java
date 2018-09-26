// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.net.URI;
import java.net.URISyntaxException;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

import org.java_websocket.client.WebSocketClient;
import org.java_websocket.handshake.ServerHandshake;

class WebSocketTransport implements Transport {
    private WebSocketClient webSocketClient;
    private OnReceiveCallBack onReceiveCallBack;
    private URI url;
    private Logger logger;
    private Map<String, String> headers;

    private static final String HTTP = "http";
    private static final String HTTPS = "https";
    private static final String WS = "ws";
    private static final String WSS = "wss";

    public WebSocketTransport(String url, Logger logger, Map<String, String> headers) throws URISyntaxException {
        this.url = formatUrl(url);
        this.logger = logger;
        this.headers = headers;
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
        return CompletableFuture.runAsync(() -> {
            logger.log(LogLevel.Debug, "Starting Websocket connection.");
            webSocketClient = createWebSocket(headers);
            try {
                if (!webSocketClient.connectBlocking()) {
                    String errorMessage = "There was an error starting the Websockets transport.";
                    logger.log(LogLevel.Debug, errorMessage);
                    throw new RuntimeException(errorMessage);
                }
            } catch (InterruptedException e) {
                String interruptedExMessage = "Connecting the Websockets transport was interrupted.";
                logger.log(LogLevel.Debug, interruptedExMessage);
                throw new RuntimeException(interruptedExMessage);
            }
            logger.log(LogLevel.Information, "WebSocket transport connected to: %s", webSocketClient.getURI());
        });
    }

    @Override
    public CompletableFuture send(String message) {
        return CompletableFuture.runAsync(() -> webSocketClient.send(message));
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
            webSocketClient.closeConnection(0, "HubConnection Stopped");
            logger.log(LogLevel.Information, "WebSocket connection stopped");
        });
    }

    private WebSocketClient createWebSocket(Map<String, String> headers) {
        return new WebSocketClient(url, headers) {
            @Override
            public void onOpen(ServerHandshake handshakedata) {
                System.out.println("Connected to " + url);
            }

            @Override
            public void onMessage(String message) {
                try {
                    onReceive(message);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }

            @Override
            public void onClose(int code, String reason, boolean remote) {
                System.out.println("Connection Closed");
            }

            @Override
            public void onError(Exception ex) {
                System.out.println("Error: " + ex.getMessage());
            }
        };
    }
}
