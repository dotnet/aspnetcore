// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

public class InvocationBindingFailureMessage extends HubMessage {
    private final String invocationId;
    private final String target;
    private final Exception exception;

    public InvocationBindingFailureMessage(String invocationId, String target, Exception exception) {
        this.invocationId = invocationId;
        this.target = target;
        this.exception = exception;
    }

    public String getInvocationId() {
        return invocationId;
    }

    public String getTarget() {
        return target;
    }

    public Exception getException() {
        return exception;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.INVOCATION_BINDING_FAILURE;
    }
}
