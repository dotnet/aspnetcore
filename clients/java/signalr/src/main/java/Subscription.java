// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import java.util.List;

public class Subscription {
    private CallbackMap handlers;
    private ActionBase action;
    private String target;
    public Subscription(CallbackMap handlers, ActionBase action, String target) {
        this.handlers = handlers;
        this.action = action;
        this.target = target;
    }

    public void unsubscribe() {
        List<ActionBase> actions = this.handlers.get(target);
        if (actions != null){
            actions.remove(action);
        }
    }
}
