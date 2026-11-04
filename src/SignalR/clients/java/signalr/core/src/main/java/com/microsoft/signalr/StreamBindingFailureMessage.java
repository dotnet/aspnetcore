// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

public class StreamBindingFailureMessage extends HubMessage {
    private final String invocationId;
    private final Exception exception;

    public StreamBindingFailureMessage(String invocationId, Exception exception) {
        this.invocationId = invocationId;
        this.exception = exception;
    }

    public String getInvocationId() {
        return invocationId;
    }

    public Exception getException() {
        return exception;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_BINDING_FAILURE;
    }
}
