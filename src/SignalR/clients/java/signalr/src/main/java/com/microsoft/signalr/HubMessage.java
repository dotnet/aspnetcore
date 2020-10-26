// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

/**
 * A base class for hub messages.
 */
 abstract class HubMessage {
    public abstract HubMessageType getMessageType();
}
