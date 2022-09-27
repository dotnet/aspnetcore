// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.locks.ReentrantLock;

class CallbackMap {
    private final Map<String, List<InvocationHandler>> handlers = new HashMap<>();
    private final ReentrantLock lock = new ReentrantLock();

    public InvocationHandler put(String target, Object action, Type... types) {
        try {
            lock.lock();
            InvocationHandler handler = new InvocationHandler(action, types);
            if (!handlers.containsKey(target)) {
                handlers.put(target, new ArrayList<>());
            }

            List<InvocationHandler> methodHandlers;
            methodHandlers = handlers.get(target);
            if (handler.getHasResult()) {
                for (InvocationHandler existingHandler : methodHandlers) {
                    if (existingHandler.getHasResult()) {
                        throw new RuntimeException(String.format("'%s' already has a value returning handler. Multiple return values are not supported.", target));
                    }
                }
            }
            methodHandlers = new ArrayList<>(methodHandlers);
            methodHandlers.add(handler);

            // replace List in handlers map
            handlers.remove(target);
            handlers.put(target, methodHandlers);
            return handler;
        } finally {
            lock.unlock();
        }
    }

    public List<InvocationHandler> get(String key) {
        try {
            lock.lock();
            return this.handlers.get(key);
        } finally {
            lock.unlock();
        }
    }

    public void remove(String key) {
        try {
            lock.lock();
            handlers.remove(key);
        } finally {
            lock.unlock();
        }
    }

    public void remove(String key, InvocationHandler handler) {
        try {
            lock.lock();
            List<InvocationHandler> handlers = this.handlers.get(key);
            if (handlers != null) {
                handlers = new ArrayList<>(handlers);
                handlers.remove(handler);

                // replace List in handlers map
                this.handlers.remove(key);
                this.handlers.put(key, handlers);
            }
        } finally {
            lock.unlock();
        }
    }
}
