// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class InvocationBindingFailureMessage extends HubMessage {
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
