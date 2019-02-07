// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import io.reactivex.Single;
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
    private volatile Boolean active = false;
    private String pollUrl;
    private String closeError;
    private Single<String> accessTokenProvider;
    private CompletableSubject receiveLoop = CompletableSubject.create();
    private ExecutorService threadPool;

    private final Logger logger = LoggerFactory.getLogger(LongPollingTransport.class);

    public LongPollingTransport(Map<String, String> headers, HttpClient client, Single<String> accessTokenProvider) {
        this.headers = headers;
        this.client = client;
        this.pollingClient = client.cloneWithTimeOut(POLL_TIMEOUT);
        this.accessTokenProvider = accessTokenProvider;
    }

    //Package private active accessor for testing.
    boolean isActive() {
        return this.active;
    }

    private void updateHeaderToken() {
        this.accessTokenProvider.flatMap((token) -> {
            if(!token.isEmpty()) {
                this.headers.put("Authorization", "Bearer " + token);
            }
            return Single.just("");
        });

    }

    @Override
    public Completable start(String url) {
        this.active = true;
        logger.info("Starting LongPolling transport");
        this.url = url;
        pollUrl = url + "&_=" + System.currentTimeMillis();
        logger.debug("Polling {}", pollUrl);
        this.updateHeaderToken();
        HttpRequest request = new HttpRequest();
        request.addHeaders(headers);
        return this.pollingClient.get(pollUrl, request).flatMapCompletable(response -> {
            if (response.getStatusCode() != 200) {
                logger.error("Unexpected response code {}.", response.getStatusCode());
                this.active = false;
                return Completable.error(new Exception("Failed to connect."));
            } else {
                this.active = true;
            }
            this.threadPool = Executors.newCachedThreadPool();
            poll(url).subscribeWith(receiveLoop);

            return Completable.complete();
        });
    }

    private Completable poll(String url) {
        if (this.active) {
            pollUrl = url + "&_=" + System.currentTimeMillis();
            logger.info("Polling {}", pollUrl);
            this.updateHeaderToken();
            HttpRequest request = new HttpRequest();
            request.addHeaders(headers);
            Completable pollingCompletable = this.pollingClient.get(pollUrl, request).flatMapCompletable(response -> {
                if (response.getStatusCode() == 204) {
                    logger.info("LongPolling transport terminated by server.");
                    this.active = false;
                } else if (response.getStatusCode() != 200) {
                    logger.error("Unexpected response code {}.", response.getStatusCode());
                    this.active = false;
                    this.closeError = "Unexpected response code " + response.getStatusCode();
                } else {
                    if(response.getContent() != null) {
                        logger.debug("Message received.");
                        threadPool.execute(() -> this.onReceive(response.getContent()));
                    } else {
                        logger.debug("Poll timed out, reissuing.");
                    }

                }
                return poll(url); });
            return pollingCompletable;
        } else {
            logger.info("Long Polling transport polling complete.");
            receiveLoop.onComplete();
            return this.stop();
        }
    }

    @Override
    public Completable send(String message) {
        if (!this.active) {
            return Completable.error(new Exception("Cannot send unless the transport is active."));
        }
        this.updateHeaderToken();
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
        this.updateHeaderToken();
        this.pollingClient.delete(this.url);
        CompletableSubject stopCompletableSubject = CompletableSubject.create();
        return this.receiveLoop.andThen(Completable.defer(() -> {
            logger.info("LongPolling transport stopped.");
            this.onClose.invoke(this.closeError);
            return Completable.complete();
        })).subscribeWith(stopCompletableSubject);
    }
}
