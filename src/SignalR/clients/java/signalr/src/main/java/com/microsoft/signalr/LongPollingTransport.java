// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import io.reactivex.Completable;
import io.reactivex.subjects.CompletableSubject;

class LongPollingTransport implements Transport {
    private OnReceiveCallBack onReceiveCallBack;
    private TransportOnClosedCallback onClose;
    private String url;
    private final HttpClient client;
    private final HttpClient pollingClient;
    private final Map<String, String> headers;
    private static final int POLL_TIMEOUT = 100*1000;
    private volatile Boolean active;
    private String pollUrl;
    private String closeError;
    private CompletableSubject pollCompletableSubject = CompletableSubject.create();
    private final Logger logger = LoggerFactory.getLogger(LongPollingTransport.class);

    public LongPollingTransport(Map<String, String> headers, HttpClient client) {
        this.headers = headers;
        this.client = client;
        this.pollingClient = client.cloneWithTimeOut(POLL_TIMEOUT);
    }

    //Package private active accessor for testing.
    boolean isActive() {
        return this.active;
    }

    @Override
    public Completable start(String url) {
        this.active = true;
        logger.info("Starting LongPolling transport");
        this.url = url;
        pollUrl = url + "&_=" + System.currentTimeMillis();
        logger.info("Polling {}", pollUrl);
        HttpRequest request = new HttpRequest();
        request.addHeaders(headers);
        return this.pollingClient.get(pollUrl, request).flatMapCompletable(response -> {
            if (response.getStatusCode() != 200) {
                logger.error("Unexpected response code {}", response.getStatusCode());
                this.active = false;
                return Completable.error(new Exception("Failed to connect."));
            } else {
                logger.info("Activating poll loop", response.getStatusCode());
                this.active = true;
            }
            poll(url).subscribeWith(pollCompletableSubject);

            return Completable.complete();
        });
    }

    private Completable poll(String url) {
        if (this.active) {
            pollUrl = url + "&_=" + System.currentTimeMillis();
            logger.info("Polling {}", pollUrl);
            HttpRequest request = new HttpRequest();
            request.addHeaders(headers);
            Completable pollingCompletable = this.pollingClient.get(pollUrl, request).flatMapCompletable(response -> {
                if (response.getStatusCode() == 204) {
                    logger.info("LongPolling transport terminated by server.");
                    this.active = false;
                } else if (response.getStatusCode() != 200) {
                    logger.error("Unexpected response code {}", response.getStatusCode());
                    this.active = false;
                    this.closeError = "Unexpected response code " + response.getStatusCode();
                } else {
                    logger.info("Message received");
                    new Thread(() -> this.onReceive(response.getContent())).start();
                }
                return poll(url); });
            return pollingCompletable;
        } else {
            logger.info("Long Polling transport polling complete.");
            pollCompletableSubject.onComplete();
            this.stop();
        }

        return Completable.complete();
    }

    @Override
    public Completable send(String message) {
        return Completable.fromSingle(this.client.post(url, message));
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
    }

    @Override
    public void onReceive(String message) {
        this.onReceiveCallBack.invoke(message);
        logger.debug("OnReceived callback has been invoked.");
    }

    @Override
    public void setOnClose(TransportOnClosedCallback onCloseCallback) {
        this.onClose = onCloseCallback;
    }

    @Override
    public Completable stop() {
        this.active = false;
        this.pollingClient.delete(this.url);
        CompletableSubject stopCompletableSubject = CompletableSubject.create();
        return this.pollCompletableSubject.andThen(Completable.defer(() -> {
            logger.info("LongPolling transport stopped.");
            this.onClose.invoke(this.closeError);
            return Completable.complete();
        })).subscribeWith(stopCompletableSubject);
    }
}
