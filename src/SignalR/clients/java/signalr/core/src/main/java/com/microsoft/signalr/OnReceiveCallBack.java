// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;

interface OnReceiveCallBack {
    void invoke(ByteBuffer message);
}
