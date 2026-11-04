// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

final class HandshakeResponseMessage {
    private final String error;

    public HandshakeResponseMessage() {
        this(null);
    }

    public HandshakeResponseMessage(String error) {
        this.error = error;
    }

    public String getHandshakeError() {
        return error;
    }
}
