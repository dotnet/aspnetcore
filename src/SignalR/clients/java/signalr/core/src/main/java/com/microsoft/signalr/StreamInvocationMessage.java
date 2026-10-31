// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.util.Collection;
import java.util.Map;

public final class StreamInvocationMessage extends InvocationMessage {

    public StreamInvocationMessage(Map<String, String> headers, String invocationId, String target, Object[] args, Collection<String> streamIds) {
        super(headers, invocationId, target, args, streamIds);
        super.type = HubMessageType.STREAM_INVOCATION.value;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_INVOCATION;
    }
}
