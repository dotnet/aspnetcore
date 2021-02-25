// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
