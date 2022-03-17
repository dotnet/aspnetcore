// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;

import io.reactivex.rxjava3.core.Completable;

interface Transport {
    Completable start(String url);
    Completable send(ByteBuffer message);
    void setOnReceive(OnReceiveCallBack callback);
    void onReceive(ByteBuffer message);
    void setOnClose(TransportOnClosedCallback onCloseCallback);
    Completable stop();
}
