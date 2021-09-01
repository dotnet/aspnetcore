// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.stream.Stream;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

class ResolveNegotiateUrlTest {
    private static Stream<Arguments> protocols() {
        return Stream.of(
                Arguments.of("http://example.com/hub/", 0, "http://example.com/hub/negotiate?negotiateVersion=0"),
                Arguments.of("http://example.com/hub", 1, "http://example.com/hub/negotiate?negotiateVersion=1"),
                Arguments.of("http://example.com/endpoint?q=my/Data", 0, "http://example.com/endpoint/negotiate?q=my/Data&negotiateVersion=0"),
                Arguments.of("http://example.com/endpoint/?q=my/Data", 1, "http://example.com/endpoint/negotiate?q=my/Data&negotiateVersion=1"),
                Arguments.of("http://example.com/endpoint/path/more?q=my/Data", 0, "http://example.com/endpoint/path/more/negotiate?q=my/Data&negotiateVersion=0"),
                Arguments.of("http://example.com/hub/?negotiateVersion=2", 0, "http://example.com/hub/negotiate?negotiateVersion=2"));
    }

    @ParameterizedTest
    @MethodSource("protocols")
    public void checkNegotiateUrl(String url, int negotiateVersion, String resolvedUrl) {
        String urlResult = Negotiate.resolveNegotiateUrl(url, negotiateVersion);
        assertEquals(resolvedUrl, urlResult);
    }
}