// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.util.Collection;
import java.util.Map;

public class InvocationMessage extends HubMessage {
    int type = HubMessageType.INVOCATION.value;
    private Map<String, String> headers;
    private final String invocationId;
    private final String target;
    private final Object[] arguments;
    private Collection<String> streamIds;

    public InvocationMessage(Map<String, String> headers, String invocationId, String target, Object[] args, Collection<String> streamIds) {
        if (headers != null && !headers.isEmpty()) {
            this.headers = headers;
        }
        this.invocationId = invocationId;
        this.target = target;
        this.arguments = args;
        if (streamIds != null && !streamIds.isEmpty()) {
            this.streamIds = streamIds;
        }
    }

    public Map<String, String> getHeaders() {
        return headers;
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

    public Collection<String> getStreamIds() {
        return streamIds;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.INVOCATION;
    }
}
