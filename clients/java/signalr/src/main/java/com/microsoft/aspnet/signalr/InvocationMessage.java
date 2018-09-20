// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

class InvocationMessage extends HubMessage {
    int type = HubMessageType.INVOCATION.value;
    protected String invocationId;
    private String target;
    private Object[] arguments;

    public InvocationMessage(String invocationId, String target, Object[] args) {
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

    public void setTarget(String target) {
        this.target = target;
    }

    public Object[] getArguments() {
        return arguments;
    }

    public void setArguments(Object[] arguments) {
        this.arguments = arguments;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.INVOCATION;
    }
}
