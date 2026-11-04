// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

/**
 * A callback to create and register on a HubConnections OnClosed method.
 */
public interface OnClosedCallback {
    void invoke(Exception exception);
}