// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

class CallbackMap {
    private Map<String, List<InvocationHandler>> handlers = new ConcurrentHashMap<>();

    public InvocationHandler put(String target, ActionBase action, Class<?>... classes) {
        InvocationHandler handler = new InvocationHandler(action, classes);
        handlers.compute(target, (key, value) -> {
            if (value == null) {
                value = new ArrayList<>();
            }
            value.add(handler);
            return value;
        });
        return handler;
    }

    public List<InvocationHandler> get(String key) {
        return handlers.get(key);
    }

    public void remove(String key) {
        handlers.remove(key);
    }
}
