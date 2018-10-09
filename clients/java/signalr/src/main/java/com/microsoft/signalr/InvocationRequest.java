// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.concurrent.CompletableFuture;

class InvocationRequest {
    private Class<?> returnType;
    private CompletableFuture<Object> pendingCall = new CompletableFuture<>();
    private String invocationId;

    InvocationRequest(Class<?> returnType, String invocationId) {
        this.returnType = returnType;
        this.invocationId = invocationId;
    }

    public void complete(CompletionMessage completion) {
        if (completion.getResult() != null) {
            pendingCall.complete(completion.getResult());
        } else {
            pendingCall.completeExceptionally(new HubException(completion.getError()));
        }
    }

    public void fail(Exception ex) {
        pendingCall.completeExceptionally(ex);
    }

    public void cancel() {
        pendingCall.cancel(false);
    }

    public CompletableFuture<Object> getPendingCall() {
        return pendingCall;
    }

    public Class<?> getReturnType() {
        return returnType;
    }

    public String getInvocationId() {
        return invocationId;
    }
}