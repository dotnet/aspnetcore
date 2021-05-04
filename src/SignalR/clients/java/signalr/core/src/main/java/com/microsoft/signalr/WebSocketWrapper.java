// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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