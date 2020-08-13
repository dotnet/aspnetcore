// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Collection;

final class StreamInvocationMessage extends InvocationMessage {

    public StreamInvocationMessage(String invocationId, String target, Object[] args) {
        super(invocationId, target, args);
        super.type = HubMessageType.STREAM_INVOCATION.value;
    }

    public StreamInvocationMessage(String invocationId, String target, Object[] args, Collection<String> streamIds) {
        super(invocationId, target, args, streamIds);
        super.type = HubMessageType.STREAM_INVOCATION.value;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_INVOCATION;
    }
}
