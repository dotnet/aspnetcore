// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class StreamInvocationMessage extends InvocationMessage {
    private final int type = HubMessageType.STREAM_INVOCATION.value;

    public StreamInvocationMessage(String invocationId, String target, Object[] arguments) {
        super(invocationId, target, arguments);
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_INVOCATION;
    }
}
