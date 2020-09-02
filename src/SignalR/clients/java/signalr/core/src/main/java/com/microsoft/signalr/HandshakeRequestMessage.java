// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

final class HandshakeRequestMessage {
    private final String protocol;
    private final int version;

    public HandshakeRequestMessage(String protocol, int version) {
        this.protocol = protocol;
        this.version = version;
    }
}
