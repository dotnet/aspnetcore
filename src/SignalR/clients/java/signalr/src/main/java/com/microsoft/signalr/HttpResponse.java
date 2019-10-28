// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public class HttpResponse {
    private final int statusCode;
    private final String statusText;
    private final String content;

    public HttpResponse(int statusCode) {
        this(statusCode, "");
    }

    public HttpResponse(int statusCode, String statusText) {
        this(statusCode, statusText, "");
    }

    public HttpResponse(int statusCode, String statusText, String content) {
        this.statusCode = statusCode;
        this.statusText = statusText;
        this.content = content;
    }

    public String getContent() {
        return content;
    }

    public int getStatusCode() {
        return statusCode;
    }

    public String getStatusText() {
        return statusText;
    }
}
