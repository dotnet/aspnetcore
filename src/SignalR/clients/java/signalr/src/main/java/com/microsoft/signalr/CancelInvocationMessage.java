// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

final class CancelInvocationMessage extends HubMessage {
    private final int type = HubMessageType.CANCEL_INVOCATION.value;
    private final String invocationId;

    public CancelInvocationMessage(String invocationId) {
        this.invocationId = invocationId;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.CANCEL_INVOCATION;
    }
}
