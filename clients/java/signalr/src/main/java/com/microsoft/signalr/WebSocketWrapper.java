// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.concurrent.CompletableFuture;
import java.util.function.BiConsumer;

abstract class WebSocketWrapper {
    public abstract CompletableFuture<Void> start();

    public abstract CompletableFuture<Void> stop();

    public abstract CompletableFuture<Void> send(String message);

    public abstract void setOnReceive(OnReceiveCallBack onReceive);

    public abstract void setOnClose(BiConsumer<Integer, String> onClose);
}