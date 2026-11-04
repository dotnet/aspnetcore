// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;
import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import io.reactivex.rxjava3.core.Completable;

class WebSocketTransport implements Transport {
    private WebSocketWrapper webSocketClient;
    private OnReceiveCallBack onReceiveCallBack;
    private TransportOnClosedCallback onClose;
    private String url;
    private HttpClient client;
    private Map<String, String> headers;
    private final Logger logger = LoggerFactory.getLogger(WebSocketTransport.class);

    private static final String HTTP = "http";
    private static final String HTTPS = "https";
    private static final String WS = "ws";
    private static final String WSS = "wss";

    public WebSocketTransport(Map<String, String> headers, HttpClient client) {
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
    public Completable start(String url) {
        this.url = formatUrl(url);
        logger.debug("Starting Websocket connection.");
        this.webSocketClient = client.createWebSocket(this.url, this.headers);
        this.webSocketClient.setOnReceive((message) -> onReceive(message));
        this.webSocketClient.setOnClose((code, reason) -> {
            if (onClose != null) {
                onClose(code, reason);
            }
        });

        return webSocketClient.start().doOnComplete(() -> logger.info("WebSocket transport connected to: {}.", this.url));
    }

    @Override
    public Completable send(ByteBuffer message) {
        return webSocketClient.send(message);
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
        logger.debug("OnReceived callback has been set.");
    }

    @Override
    public void onReceive(ByteBuffer message) {
        this.onReceiveCallBack.invoke(message);
    }

    @Override
    public void setOnClose(TransportOnClosedCallback onCloseCallback) {
        this.onClose = onCloseCallback;
    }

    @Override
    public Completable stop() {
        Completable stop = webSocketClient.stop();
        stop.onErrorComplete().subscribe(() -> logger.info("WebSocket connection stopped."));
        return stop;
    }

    void onClose(Integer code, String reason) {
        if (code == null || code != 1000) {
            onClose.invoke(reason);
        }
        else {
            onClose.invoke(null);
        }
    }
}
