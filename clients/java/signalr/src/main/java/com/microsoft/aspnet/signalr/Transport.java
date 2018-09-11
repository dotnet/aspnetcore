// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

interface Transport {
    void start() throws Exception;
    void send(String message) throws Exception;
    void setOnReceive(OnReceiveCallBack callback);
    void onReceive(String message) throws Exception;
    void stop();
}
