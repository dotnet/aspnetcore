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

    @Override
    public CompletableFuture start(String url) {
        this.url = url;
        return CompletableFuture.completedFuture(null);
    }

    @Override
    public CompletableFuture send(String message) {
        sentMessages.add(message);
        return CompletableFuture.completedFuture(null);
    }

    @Override
    public void setOnReceive(OnReceiveCallBack callback) {
        this.onReceiveCallBack = callback;
    }

    @Override
    public void onReceive(String message) throws Exception {
        this.onReceiveCallBack.invoke(message);
    }

    @Override
    public void setOnClose(Consumer<String> onCloseCallback) {
        this.onClose = onCloseCallback;
    }

    @Override
    public CompletableFuture stop() {
        onClose.accept(null);
        return CompletableFuture.completedFuture(null);
    }

    public void stopWithError(String errorMessage) {
        onClose.accept(errorMessage);
    }

    public void receiveMessage(String message) throws Exception {
        this.onReceive(message);
    }

    public String[] getSentMessages() {
        return sentMessages.toArray(new String[sentMessages.size()]);
    }

    public String getUrl() {
        return this.url;
    }
}
