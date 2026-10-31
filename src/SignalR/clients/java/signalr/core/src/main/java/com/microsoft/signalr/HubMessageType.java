// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

public enum HubMessageType {
    INVOCATION(1),
    STREAM_ITEM(2),
    COMPLETION(3),
    STREAM_INVOCATION(4),
    CANCEL_INVOCATION(5),
    PING(6),
    CLOSE(7),
    INVOCATION_BINDING_FAILURE(-1),
    STREAM_BINDING_FAILURE(-2);

    public int value;
    HubMessageType(int id) { this.value = id; }
}
