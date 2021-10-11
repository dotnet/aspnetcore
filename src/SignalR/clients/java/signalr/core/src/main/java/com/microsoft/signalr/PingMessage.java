// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

public class PingMessage extends HubMessage
{
    private final int type = HubMessageType.PING.value;

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