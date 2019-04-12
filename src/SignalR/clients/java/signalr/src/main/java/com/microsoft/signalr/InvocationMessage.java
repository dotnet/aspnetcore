// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Collection;

class InvocationMessage extends HubMessage {
    int type = HubMessageType.INVOCATION.value;
    private final String invocationId;
    private final String target;
    private final Object[] arguments;
    private Collection<String> streamIds;

    public InvocationMessage(String invocationId, String target, Object[] args) {
        this(invocationId, target, args, null);
    }

    public InvocationMessage(String invocationId, String target, Object[] args, Collection<String> streamIds) {
        this.invocationId = invocationId;
        this.target = target;
        this.arguments = args;
        if (streamIds != null && !streamIds.isEmpty()) {
            this.streamIds = streamIds;
        }
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
        return HubMessageType.INVOCATION;
    }
}
