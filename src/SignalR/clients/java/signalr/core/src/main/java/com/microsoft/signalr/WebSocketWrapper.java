// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;

import io.reactivex.rxjava3.core.Completable;

abstract class WebSocketWrapper {
    public abstract Completable start();

    public abstract Completable stop();

    public abstract Completable send(ByteBuffer message);

    public abstract void setOnReceive(OnReceiveCallBack onReceive);

    public abstract void setOnClose(WebSocketOnClosedCallback onClose);
}