// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

class TestUtils {
    static HubConnection createHubConnection(String url) {
        return createHubConnection(url, new MockTransport(), new NullLogger(), true, new TestHttpClient());
    }

    static HubConnection createHubConnection(String url, Transport transport) {
        return createHubConnection(url, transport, new NullLogger(), true, new TestHttpClient());
    }

    static HubConnection createHubConnection(String url, Transport transport, Logger logger, boolean skipNegotiate, HttpClient client) {
        HttpConnectionOptions options = new HttpConnectionOptions();
        options.setTransport(transport);
        options.setLogger(logger);
        options.setSkipNegotiate(skipNegotiate);
        options.setHttpClient(client);
        HubConnectionBuilder builder = new HubConnectionBuilder();
        return builder.withUrl(url, options).build();
    }
}