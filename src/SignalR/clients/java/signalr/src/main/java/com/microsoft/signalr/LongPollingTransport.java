// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicBoolean;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import io.reactivex.Completable;
import io.reactivex.Single;
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
    private ExecutorService onReceiveThread;
    private AtomicBoolean stopCalled = new AtomicBoolean(false);

    private final Logger logger = LoggerFactory.getLogger(LongPollingTransport.class);

    public LongPollingTransport(Map<String, String> headers, HttpClient client, Single<String> accessTokenProvider) {
        this.headers = headers;
        this.client = client;
        this.pollingClient = client.cloneWithTimeOut(POLL_TIMEOUT);
        this.accessTokenProvider = accessTokenProvider;
        this.onReceiveThread = Executors.newSingleThreadExecutor();
    }

    //Package private active accessor for testing.
    boolean isActive() {
        return this.active;
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
        this.active = true;
        logger.debug("Starting LongPolling transport.");
        this.url = url;
        pollUrl = url + "&_=" + System.currentTimeMillis();
        logger.debug("Polling {}.", pollUrl);
        return this.updateHeaderToken().flatMapCompletable((r) -> {
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
                threadPool.execute(() -> poll(url).subscribeWith(receiveLoop));

                return Completable.complete();
            });
        });
    }

    private Completable poll(String url) {
        if (this.active) {
            pollUrl = url + "&_=" + System.currentTimeMillis();
            logger.debug("Polling {}.", pollUrl);
            return this.updateHeaderToken().flatMapCompletable((x) -> {
                HttpRequest request = new HttpRequest();
                request.addHeaders(headers);
                Completable pollingCompletable = this.pollingClient.get(pollUrl, request).flatMapCompletable(response -> {
                    if (response.getStatusCode() == 204) {
                        logger.info("LongPolling transport terminated by server.");
                        this.active = false;
                    } else if (response.getStatusCode() != 200) {
                        logger.error("Unexpected response code {}.", response.getStatusCode());
                        this.active = false;
                        this.closeError = "Unexpected response code " + response.getStatusCode() + ".";
                    } else {
                        if (response.getContent() != null) {
                            logger.debug("Message received.");
                            onReceiveThread.submit(() ->this.onReceive(response.getContent()));
                        } else {
                            logger.debug("Poll timed out, reissuing.");
                        }
                    }
                    return poll(url);
                });

                return pollingCompletable;
            });
        } else {
            logger.debug("Long Polling transport polling complete.");
            receiveLoop.onComplete();
            if (!stopCalled.get()) {
                return this.stop();
            }
            return Completable.complete();
        }
    }

    @Override
    public Completable send(String message) {
        if (!this.active) {
            return Completable.error(new Exception("Cannot send unless the transport is active."));
        }
        return this.updateHeaderToken().flatMapCompletable((x) -> {
            HttpRequest request = new HttpRequest();
            request.addHeaders(headers);
            return Completable.fromSingle(this.client.post(url, message, request));
        });
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
        if (!stopCalled.get()) {
            this.stopCalled.set(true);
            this.active = false;
            return this.updateHeaderToken().flatMapCompletable((x) -> {
                HttpRequest request = new HttpRequest();
                request.addHeaders(headers);
                this.pollingClient.delete(this.url, request);
                CompletableSubject stopCompletableSubject = CompletableSubject.create();
                return this.receiveLoop.andThen(Completable.defer(() -> {
                    logger.info("LongPolling transport stopped.");
                    this.onClose.invoke(this.closeError);
                    return Completable.complete();
                })).subscribeWith(stopCompletableSubject);
            });
        }
        return Completable.complete();
    }
}
