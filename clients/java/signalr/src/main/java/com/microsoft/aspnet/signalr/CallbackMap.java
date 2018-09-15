// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

class CallbackMap {
    private ConcurrentHashMap<String, List<InvocationHandler>> handlers = new ConcurrentHashMap<>();

    public InvocationHandler put(String target, ActionBase action, ArrayList<Class<?>> classes) {
        InvocationHandler handler = new InvocationHandler(action, Collections.unmodifiableList(classes));

        handlers.computeIfPresent(target, (methodName, handlerList) -> {
            handlerList.add(handler);
            return handlerList;
        });
        handlers.computeIfAbsent(target, (ac) -> new ArrayList<>(Arrays.asList(handler)));
        return handler;
    }

    public Boolean containsKey(String key) {
        return handlers.containsKey(key);
    }

    public List<InvocationHandler> get(String key) {
        return handlers.get(key);
    }

    public void remove(String key) {
        handlers.remove(key);
    }
}
