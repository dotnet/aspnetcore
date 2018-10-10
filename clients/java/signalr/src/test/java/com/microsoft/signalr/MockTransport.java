// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.ArrayList;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

class MockTransport implements Transport {
    private OnReceiveCallBack onReceiveCallBack;
    private ArrayList<String> sentMessages = new ArrayList<>();
    private String url;
    private Consumer<String> onClose;
    final private boolean ignorePings;
    final private boolean autoHandshake;

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
    public CompletableFuture<Void> start(String url) {
        this.url = url;
        if (autoHandshake) {
            try {
                onReceiveCallBack.invoke("{}" + RECORD_SEPARATOR);
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        }
        return CompletableFuture.completedFuture(null);
    }

    @Override
    public CompletableFuture<Void> send(String message) {
        if (!(ignorePings && message.equals("{\"type\":6}" + RECORD_SEPARATOR))) {
            sentMessages.add(message);
        }
        return CompletableFuture.completedFuture(null);
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
    public void setOnClose(Consumer<String> onCloseCallback) {
        this.onClose = onCloseCallback;
    }

    @Override
    public CompletableFuture<Void> stop() {
        onClose.accept(null);
        return CompletableFuture.completedFuture(null);
    }

    public void stopWithError(String errorMessage) {
        onClose.accept(errorMessage);
    }

    public void receiveMessage(String message) {
        this.onReceive(message);
    }

    public String[] getSentMessages() {
        return sentMessages.toArray(new String[sentMessages.size()]);
    }

    public String getUrl() {
        return this.url;
    }
}
