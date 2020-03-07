// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.ArrayList;

import io.reactivex.Completable;
import io.reactivex.subjects.CompletableSubject;
import io.reactivex.subjects.SingleSubject;

class MockTransport implements Transport {
    private OnReceiveCallBack onReceiveCallBack;
    private ArrayList<String> sentMessages = new ArrayList<>();
    private String url;
    private TransportOnClosedCallback onClose;
    final private boolean ignorePings;
    final private boolean autoHandshake;
    final private CompletableSubject startSubject = CompletableSubject.create();
    final private CompletableSubject stopSubject = CompletableSubject.create();
    private SingleSubject<String> sendSubject = SingleSubject.create();

    private static final String RECORD_SEPARATOR = "\u001e";

    public MockTransport() {
        this(true, true);
    }

    public MockTransport(boolean autoHandshake) {
        this(autoHandshake, true);
    }

    public MockTransport(boolean autoHandshake, boolean ignorePings) {
        this.autoHandshake = autoHandshake;
        this.ignorePings = ignorePings;
    }

    @Override
    public Completable start(String url) {
        this.url = url;
        if (autoHandshake) {
            try {
                onReceiveCallBack.invoke("{}" + RECORD_SEPARATOR);
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        }
        startSubject.onComplete();
        return startSubject;
    }

    @Override
    public Completable send(String message) {
        if (!(ignorePings && message.equals("{\"type\":6}" + RECORD_SEPARATOR))) {
            sentMessages.add(message);
            sendSubject.onSuccess(message);
            sendSubject = SingleSubject.create();
        }
        return Completable.complete();
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
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
        onClose.invoke(null);
        stopSubject.onComplete();
        return stopSubject;
    }

    public void stopWithError(String errorMessage) {
        onClose.invoke(errorMessage);
    }

    public void receiveMessage(String message) {
        this.onReceive(message);
    }

    public String[] getSentMessages() {
        return sentMessages.toArray(new String[sentMessages.size()]);
    }

    public SingleSubject<String> getNextSentMessage() {
        return sendSubject;
    }

    public String getUrl() {
        return this.url;
    }

    public Completable getStartTask() {
        return startSubject;
    }

    public Completable getStopTask() {
        return stopSubject;
    }
}
