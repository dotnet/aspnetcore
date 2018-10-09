// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class PingMessage extends HubMessage
{
    private static PingMessage instance = new PingMessage();

    private PingMessage()
    {
    }

    public static PingMessage getInstance() {return instance;}

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.PING;
    }
}