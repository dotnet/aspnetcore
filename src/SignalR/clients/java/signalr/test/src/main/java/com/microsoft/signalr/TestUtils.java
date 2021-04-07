// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import com.microsoft.signalr.messagepack.MessagePackHubProtocol;

class TestUtils {
    
    static ByteBuffer emptyByteBuffer = stringToByteBuffer("");

    static HubConnection createHubConnection(String url) {
        return createHubConnection(url, new MockTransport(true), true, new TestHttpClient(), false);
    }

    static HubConnection createHubConnection(String url, Transport transport) {
        return createHubConnection(url, transport, true, new TestHttpClient(), false);
    }
    
    static HubConnection createHubConnection(String url, boolean withMessagePack) {
        return createHubConnection(url, new MockTransport(true), true, new TestHttpClient(), withMessagePack);
    }
    
    static HubConnection createHubConnection(String url, Transport transport, boolean withMessagePack) {
        return createHubConnection(url, transport, true, new TestHttpClient(), withMessagePack);
    }

    static HubConnection createHubConnection(String url, Transport transport, boolean skipNegotiate, HttpClient client, boolean withMessagePack) {
        HttpHubConnectionBuilder builder = HubConnectionBuilder.create(url)
            .withTransportImplementation(transport)
            .withHttpClient(client)
            .shouldSkipNegotiate(skipNegotiate);
        
        if (withMessagePack) {
            builder = builder.withHubProtocol(new MessagePackHubProtocol());
        }

        return builder.build();
    }
    
    static String byteBufferToString(ByteBuffer buffer) {
        return new String(buffer.array(), StandardCharsets.UTF_8);
    }
    
    static ByteBuffer stringToByteBuffer(String s) {
        return ByteBuffer.wrap(s.getBytes(StandardCharsets.UTF_8));
    }
}
