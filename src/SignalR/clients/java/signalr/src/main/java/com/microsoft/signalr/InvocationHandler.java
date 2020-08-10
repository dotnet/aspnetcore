// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.util.Arrays;
import java.util.List;

class InvocationHandler {
    private final List<TypeAndClass> types;
    private final ActionBase action;

    InvocationHandler(ActionBase action, TypeAndClass... types) {
        this.action = action;
        this.types = Arrays.asList(types);
    }

    public List<TypeAndClass> getTypes() {
        return types;
    }

    public ActionBase getAction() {
        return action;
    }
}