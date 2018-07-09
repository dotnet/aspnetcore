// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

public class CallbackMap {
    private ConcurrentHashMap<String, List<ActionBase>> handlers = new ConcurrentHashMap<>();

    public void put(String target, ActionBase action) {
        handlers.computeIfPresent(target, (methodName, handlerList) -> {
            handlerList.add(action);
            return handlerList;
        });
        handlers.computeIfAbsent(target, (ac) -> new ArrayList<>(Arrays.asList(action)));
    }

    public Boolean containsKey(String key){
        return handlers.containsKey(key);
    }

    public List<ActionBase> get(String key){
        return handlers.get(key);
    }
}

