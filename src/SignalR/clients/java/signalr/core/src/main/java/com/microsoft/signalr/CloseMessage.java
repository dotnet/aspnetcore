// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public final class CloseMessage extends HubMessage {
    private final String error;
    private final boolean allowReconnect;

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.CLOSE;
    }

    public CloseMessage() {
        this(null, false);
    }

    public CloseMessage(String error) {
        this(error, false);
    }
    
    public CloseMessage(boolean allowReconnect) {
        this(null, allowReconnect);
    }

    public CloseMessage(String error, boolean allowReconnect) {
        this.error = error;
        this.allowReconnect = allowReconnect;
    }

    public String getError() {
        return this.error;
    }
    
    public boolean getAllowReconnect() {
        return this.allowReconnect;
    }
}
