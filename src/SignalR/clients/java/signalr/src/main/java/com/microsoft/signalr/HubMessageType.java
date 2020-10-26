// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

enum HubMessageType {
    INVOCATION(1),
    STREAM_ITEM(2),
    COMPLETION(3),
    STREAM_INVOCATION(4),
    CANCEL_INVOCATION(5),
    PING(6),
    CLOSE(7),
    INVOCATION_BINDING_FAILURE(-1);

    public int value;
    HubMessageType(int id) { this.value = id; }
}
