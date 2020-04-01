// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import io.reactivex.Completable;

interface Transport {
    Completable start(String url);
    Completable send(String message);
    void setOnReceive(OnReceiveCallBack callback);
    void onReceive(String message);
    void setOnClose(TransportOnClosedCallback onCloseCallback);
    Completable stop();
}
