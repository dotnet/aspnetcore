// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
