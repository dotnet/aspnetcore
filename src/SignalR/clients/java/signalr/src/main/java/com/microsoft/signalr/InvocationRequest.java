// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.concurrent.CancellationException;

import io.reactivex.Single;
import io.reactivex.subjects.SingleSubject;

class InvocationRequest {
    private final Class<?> returnType;
    private final SingleSubject<Object> pendingCall = SingleSubject.create();
    private final String invocationId;

    InvocationRequest(Class<?> returnType, String invocationId) {
        this.returnType = returnType;
        this.invocationId = invocationId;
    }

    public void complete(CompletionMessage completion) {
        if (completion.getResult() != null) {
            pendingCall.onSuccess(completion.getResult());
        } else {
            pendingCall.onError(new HubException(completion.getError()));
        }
    }

    public void fail(Exception ex) {
        pendingCall.onError(ex);
    }

    public void cancel() {
        pendingCall.onError(new CancellationException("Invocation was canceled."));
    }

    public Single<Object> getPendingCall() {
        return pendingCall;
    }

    public Class<?> getReturnType() {
        return returnType;
    }

    public String getInvocationId() {
        return invocationId;
    }
}