// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

class TestUtils {

    static HubConnection createHubConnection(String url) {
        return createHubConnection(url, new MockTransport(true), true, new TestHttpClient(), new JsonHubProtocol());
    }

    static HubConnection createHubConnection(String url, Transport transport) {
        return createHubConnection(url, transport, true, new TestHttpClient(), new JsonHubProtocol());
    }
    
    static HubConnection createHubConnection(String url, HubProtocol protocol) {
        return createHubConnection(url, new MockTransport(true), true, new TestHttpClient(), protocol);
    }
    
    static HubConnection createHubConnection(String url, Transport transport, HubProtocol protocol) {
        return createHubConnection(url, transport, true, new TestHttpClient(), protocol);
    }

    static HubConnection createHubConnection(String url, Transport transport, boolean skipNegotiate, HttpClient client, HubProtocol protocol) {
        HttpHubConnectionBuilder builder = HubConnectionBuilder.create(url)
            .withTransportImplementation(transport)
            .withHttpClient(client)
            .withProtocol(protocol)
            .shouldSkipNegotiate(skipNegotiate);

        return builder.build();
    }
    
    static String ByteBufferToString(ByteBuffer buffer) {
        return new String(buffer.array(), StandardCharsets.UTF_8);
    }
    
    static ByteBuffer StringToByteBuffer(String s) {
        return ByteBuffer.wrap(s.getBytes(StandardCharsets.UTF_8));
    }
}
