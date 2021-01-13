// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        List<InvocationHandler> handler = this.handlers.get(target);
        if (handler != null) {
            handler.remove(this.handler);
        }
    }
}
