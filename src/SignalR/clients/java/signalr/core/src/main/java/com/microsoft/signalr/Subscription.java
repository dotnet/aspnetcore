// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.util.List;

/**
 * Represents the registration of a handler for a client method.
 */
public class Subscription {
    private final CallbackMap handlers;
    private final InvocationHandler handler;
    private final String target;

    Subscription(CallbackMap handlers, InvocationHandler handler, String target) {
        this.handlers = handlers;
        this.handler = handler;
        this.target = target;
    }

    /**
     * Removes the client method handler represented by this subscription.
     */
    public void unsubscribe() {
        this.handlers.remove(this.target, this.handler);
    }
}
