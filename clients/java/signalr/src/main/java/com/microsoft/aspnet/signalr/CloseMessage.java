// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

class CloseMessage extends HubMessage {
    private String error;

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.CLOSE;
    }

    public CloseMessage() {
    }

    public CloseMessage(String error) {
        this.error = error;
    }

    public String getError() {
        return this.error;
    }
}
