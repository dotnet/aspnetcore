// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.nio.ByteBuffer;

import io.reactivex.Completable;

interface Transport {
    Completable start(String url);
    Completable send(ByteBuffer message);
    void setOnReceive(OnReceiveCallBack callback);
    void onReceive(ByteBuffer message);
    void setOnClose(TransportOnClosedCallback onCloseCallback);
    Completable stop();
}
