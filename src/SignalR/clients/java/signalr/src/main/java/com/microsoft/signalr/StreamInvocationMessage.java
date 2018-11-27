// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

final class StreamInvocationMessage extends HubMessage {
    private final int type = HubMessageType.STREAM_INVOCATION.value;
    private final String invocationId;
    private final String target;
    private final Object[] arguments;

    public StreamInvocationMessage(String invocationId, String target, Object[] args) {
        this.invocationId = invocationId;
        this.target = target;
        this.arguments = args;
    }

    public String getInvocationId() {
        return invocationId;
    }

    public String getTarget() {
        return target;
    }

    public Object[] getArguments() {
        return arguments;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_INVOCATION;
    }
}
