// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

/**
 * A protocol abstraction for communicating with SignalR hubs.
 */
interface HubProtocol {
    String getName();
    int getVersion();
    TransferFormat getTransferFormat();

    /**
     * Creates a new list of {@link HubMessage}s.
     * @param message A string representation of one or more {@link HubMessage}s.
     * @return A list of {@link HubMessage}s.
     */
    HubMessage[] parseMessages(String message, InvocationBinder binder);

    /**
     * Writes the specified {@link HubMessage} to a String.
     * @param message The message to write.
     * @return A string representation of the message.
     */
    String writeMessage(HubMessage message);
}
