// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public class HubException extends Exception {
    public HubException() {
    }

    public HubException(String errorMessage) {
        super(errorMessage);
    }

    public HubException(String errorMessage, Exception innerException) {
        super(errorMessage, innerException);
    }
}
