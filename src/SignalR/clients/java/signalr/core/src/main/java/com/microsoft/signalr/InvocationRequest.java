// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.concurrent.CancellationException;

import io.reactivex.rxjava3.subjects.ReplaySubject;
import io.reactivex.rxjava3.subjects.Subject;

class InvocationRequest {
    private final Type returnType;
    private final Subject<Object> pendingCall = ReplaySubject.create();
    private final String invocationId;

    InvocationRequest(Type returnType, String invocationId) {
        this.returnType = returnType;
        this.invocationId = invocationId;
    }

    public void complete(CompletionMessage completion) {
        if (completion.getError() == null) {
            if (completion.getResult() != null) {
                pendingCall.onNext(completion.getResult());
            }
            pendingCall.onComplete();
        } else {
            pendingCall.onError(new HubException(completion.getError()));
        }
    }

    public void addItem(StreamItem streamItem) {
        if (streamItem.getItem() != null) {
            pendingCall.onNext(streamItem.getItem());
        }
    }

    public void fail(Exception ex) {
        pendingCall.onError(ex);
    }

    public void cancel() {
        pendingCall.onError(new CancellationException("Invocation was canceled."));
    }

    public Subject<Object> getPendingCall() {
        return pendingCall;
    }

    public Type getReturnType() {
        return returnType;
    }

    public String getInvocationId() {
        return invocationId;
    }
}