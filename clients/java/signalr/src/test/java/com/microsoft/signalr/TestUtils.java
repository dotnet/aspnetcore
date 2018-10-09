// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class TestUtils {
    static HubConnection createHubConnection(String url) {
        return createHubConnection(url, new MockTransport(), new NullLogger(), true, new TestHttpClient());
    }

    static HubConnection createHubConnection(String url, Transport transport) {
        return createHubConnection(url, transport, new NullLogger(), true, new TestHttpClient());
    }

    static HubConnection createHubConnection(String url, Transport transport, Logger logger, boolean skipNegotiate, HttpClient client) {
        HttpHubConnectionBuilder builder = HubConnectionBuilder.create(url)
                .withTransport(transport)
                .withHttpClient(client)
                .shouldSkipNegotiate(skipNegotiate)
                .withLogger(logger);

        return builder.build();
    }
}