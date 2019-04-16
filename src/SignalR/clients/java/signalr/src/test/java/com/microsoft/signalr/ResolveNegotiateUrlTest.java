// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.stream.Stream;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

class ResolveNegotiateUrlTest {
    private static Stream<Arguments> protocols() {
        return Stream.of(
                Arguments.of("http://example.com/hub/", "http://example.com/hub/negotiate"),
                Arguments.of("http://example.com/hub", "http://example.com/hub/negotiate"),
                Arguments.of("http://example.com/endpoint?q=my/Data", "http://example.com/endpoint/negotiate?q=my/Data"),
                Arguments.of("http://example.com/endpoint/?q=my/Data", "http://example.com/endpoint/negotiate?q=my/Data"),
                Arguments.of("http://example.com/endpoint/path/more?q=my/Data", "http://example.com/endpoint/path/more/negotiate?q=my/Data"));
    }

    @ParameterizedTest
    @MethodSource("protocols")
    public void checkNegotiateUrl(String url, String resolvedUrl) {
        String urlResult = Negotiate.resolveNegotiateUrl(url);
        assertEquals(resolvedUrl, urlResult);
    }
}