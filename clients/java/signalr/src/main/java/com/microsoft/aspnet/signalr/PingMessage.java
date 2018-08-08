// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

public class PingMessage extends HubMessage {

    int type = HubMessageType.PING.value;

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.PING;
    }
}
