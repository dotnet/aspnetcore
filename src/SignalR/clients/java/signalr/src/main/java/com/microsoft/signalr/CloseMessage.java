// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

final class CloseMessage extends HubMessage {
    private final String error;

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.CLOSE;
    }

    public CloseMessage() {
        this(null);
    }

    public CloseMessage(String error) {
        this.error = error;
    }

    public String getError() {
        return this.error;
    }
}
