// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

interface Transport {
    CompletableFuture<Void> start(String url);
    CompletableFuture<Void> send(String message);
    void setOnReceive(OnReceiveCallBack callback);
    void onReceive(String message) throws Exception;
    void setOnClose(Consumer<String> onCloseCallback);
    CompletableFuture<Void> stop();
}
