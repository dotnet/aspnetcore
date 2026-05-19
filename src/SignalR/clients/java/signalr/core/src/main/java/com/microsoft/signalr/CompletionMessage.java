// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.util.Map;

public final class CompletionMessage extends HubMessage {
    private final int type = HubMessageType.COMPLETION.value;
    private Map<String, String> headers;
    private final String invocationId;
    private final Object result;
    private final String error;

    public CompletionMessage(Map<String, String> headers, String invocationId, Object result, String error) {
        if (headers != null && !headers.isEmpty()) {
            this.headers = headers;
        }
        if (error != null && result != null) {
            throw new IllegalArgumentException("Expected either 'error' or 'result' to be provided, but not both.");
        }
        this.invocationId = invocationId;
        this.result = result;
        this.error = error;
    }

    public Map<String, String> getHeaders() {
        return headers;
    }

    public Object getResult() {
        return result;
    }

    public String getError() {
        return error;
    }

    public String getInvocationId() {
        return invocationId;
    }

    @Override
    public HubMessageType getMessageType() {
        return HubMessageType.values()[type - 1];
    }
}
