// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

public class InvocationMessage extends HubMessage {
    int type = HubMessageType.INVOCATION.value;
    String invocationId;
    String target;
    Object[] arguments;

    public InvocationMessage(String target, Object[] args) {
        this.target = target;
        arguments = args;
    }

    public String getInvocationId() {
        return invocationId;
    }

    public void setInvocationId(String invocationId) {
        this.invocationId = invocationId;
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
    HubMessageType getMessageType() {
        return HubMessageType.INVOCATION;
    }
}
