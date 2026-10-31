// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

final class HandshakeRequestMessage {
    private final String protocol;
    private final int version;

    public HandshakeRequestMessage(String protocol, int version) {
        this.protocol = protocol;
        this.version = version;
    }
}
