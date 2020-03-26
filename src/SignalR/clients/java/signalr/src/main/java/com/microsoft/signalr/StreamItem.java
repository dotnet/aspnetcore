// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

final class StreamItem extends HubMessage {
    private final int type = HubMessageType.STREAM_ITEM.value;
    private final String invocationId;
    private final Object item;

    public StreamItem(String invocationId, Object item) {
        this.invocationId = invocationId;
        this.item = item;
    }

    public String getInvocationId() {
        return invocationId;
    }

    public Object getItem() {
        return item;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.STREAM_ITEM;
    }
}
