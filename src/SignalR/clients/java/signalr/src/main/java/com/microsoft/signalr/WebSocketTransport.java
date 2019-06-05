// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Map;

import io.reactivex.Single;
import io.reactivex.subjects.CompletableSubject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import io.reactivex.Completable;

class WebSocketTransport implements Transport {
    private WebSocketWrapper webSocketClient;
    private OnReceiveCallBack onReceiveCallBack;
    private TransportOnClosedCallback onClose;
    private String url;
    private HttpClient client;
    private Map<String, String> headers;
    private Single<String> accessTokenProvider;

    private final Logger logger = LoggerFactory.getLogger(WebSocketTransport.class);

    private static final String HTTP = "http";
    private static final String HTTPS = "https";
    private static final String WS = "ws";
    private static final String WSS = "wss";

    public WebSocketTransport(Map<String, String> headers, HttpClient client) {
        this(headers, client, Single.just(""));
    }

    public WebSocketTransport(Map<String, String> headers, HttpClient client, Single<String> accessTokenProvider) {
        this.client = client;
        this.headers = headers;
        this.accessTokenProvider = accessTokenProvider;
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

    private Single updateHeaderToken() {
        return this.accessTokenProvider.flatMap((token) -> {
            if (!token.isEmpty()) {
                this.headers.put("Authorization", "Bearer " + token);
            }
            return Single.just("");
        });
    }

    @Override
    public Completable start(String url) {
        this.url = formatUrl(url);
        CompletableSubject start = CompletableSubject.create();
        logger.debug("Starting Websocket connection.");
        return this.updateHeaderToken().flatMapCompletable((r) -> {
            this.webSocketClient = client.createWebSocket(this.url, this.headers);
            this.webSocketClient.setOnReceive((message) -> onReceive(message));
            this.webSocketClient.setOnClose((code, reason) -> {
                if (onClose != null) {
                    onClose(code, reason);
                }
            });

            return webSocketClient.start().doOnComplete(() -> logger.info("WebSocket transport connected to: {}.", this.url));
        }).subscribeWith(start);

    }

    @Override
    public Completable send(String message) {
        return webSocketClient.send(message);
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
        logger.debug("OnReceived callback has been set.");
    }

    @Override
    public void onReceive(String message) {
        this.onReceiveCallBack.invoke(message);
    }

    @Override
    public void setOnClose(TransportOnClosedCallback onCloseCallback) {
        this.onClose = onCloseCallback;
    }

    @Override
    public Completable stop() {
        return webSocketClient.stop().doOnEvent(t -> logger.info("WebSocket connection stopped."));
    }

    void onClose(Integer code, String reason) {
        logger.info("WebSocket connection stopping with " +
                "code {} and reason '{}'.", code, reason);
        if (code == null || code != 1000) {
            onClose.invoke(reason);
        }
        else {
            onClose.invoke(null);
        }
    }
}
