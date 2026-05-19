// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.util.HashMap;
import java.util.stream.Stream;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;


class WebSocketTransportUrlFormatTest {
    private static Stream<Arguments> protocols() {
        return Stream.of(
                Arguments.of("http://example.com", "ws://example.com"),
                Arguments.of("https://example.com", "wss://example.com"),
                Arguments.of("ws://example.com", "ws://example.com"),
                Arguments.of("wss://example.com", "wss://example.com"));
    }

    @ParameterizedTest
    @MethodSource("protocols")
    public void checkWebsocketUrlProtocol(String url, String expectedUrl) {
        WebSocketTransport webSocketTransport = new WebSocketTransport(new HashMap<>(), new TestHttpClient());
        try {
            webSocketTransport.start(url);
        } catch (Exception e) {}
        assertEquals(expectedUrl, webSocketTransport.getUrl());
    }
}
