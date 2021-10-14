// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;
import java.util.ArrayList;

import io.reactivex.rxjava3.core.Completable;
import io.reactivex.rxjava3.subjects.CompletableSubject;
import io.reactivex.rxjava3.subjects.SingleSubject;

class MockTransport implements Transport {
    private OnReceiveCallBack onReceiveCallBack;
    private ArrayList<ByteBuffer> sentMessages = new ArrayList<>();
    private String url;
    private TransportOnClosedCallback onClose;
    final private boolean ignorePings;
    final private boolean autoHandshake;
    final private CompletableSubject startSubject = CompletableSubject.create();
    final private CompletableSubject stopSubject = CompletableSubject.create();
    private SingleSubject<ByteBuffer> sendSubject = SingleSubject.create();

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
                onReceiveCallBack.invoke(TestUtils.stringToByteBuffer("{}" + RECORD_SEPARATOR));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        }
        startSubject.onComplete();
        return startSubject;
    }

    @Override
    public Completable send(ByteBuffer message) {
        if (!(ignorePings && isPing(message))) {
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
    public void onReceive(ByteBuffer message) {
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
        this.onReceive(TestUtils.stringToByteBuffer(message));
    }

    public void receiveMessage(ByteBuffer message) {
        this.onReceive(message);
    }

    public ByteBuffer[] getSentMessages() {
        return sentMessages.toArray(new ByteBuffer[sentMessages.size()]);
    }

    public SingleSubject<ByteBuffer> getNextSentMessage() {
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

    private boolean isPing(ByteBuffer message) {
    	return (TestUtils.byteBufferToString(message).equals("{\"type\":6}" + RECORD_SEPARATOR) ||
    	       (message.array()[0] == 2 && message.array()[1] == -111 && message.array()[2] == 6));
    }
}
