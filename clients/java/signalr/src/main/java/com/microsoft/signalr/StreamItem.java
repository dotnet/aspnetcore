// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

final class StreamItem extends HubMessage {
    private final int type = HubMessageType.STREAM_ITEM.value;
    private final String invocationId;
    private final Object result;

    public StreamItem(String invocationId, Object result) {
        this.invocationId = invocationId;
        this.result = result;
    }

    public String getInvocationId() {
        return invocationId;
    }

    public Object getResult() {
        return result;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_ITEM;
    }
}
