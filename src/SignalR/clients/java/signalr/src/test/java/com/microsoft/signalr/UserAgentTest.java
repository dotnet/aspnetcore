// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.stream.Stream;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

public class UserAgentTest {

    private static Stream<Arguments> OperatingSystems() {
        return Stream.of(
                Arguments.of("Windows XP", "Windows NT"),
                Arguments.of("wInDoWs 95", "Windows NT"),
                Arguments.of("Macintosh", "Mac"),
                Arguments.of("Linux", "Linux"),
                Arguments.of("unix", "Linux"),
                Arguments.of("", ""),
                Arguments.of("1234", ""));
    }

    @ParameterizedTest
    @MethodSource("OperatingSystems")
    public void getOSName(String osInput, String os) {
        assertEquals(os, UserAgentHelper.findOSName(osInput));
    }

    private static Stream<Arguments> Versions() {
        return Stream.of(
                Arguments.of("1.0.0", "1.0.0"),
                Arguments.of("3.1.4-preview9-12345", "3.1.4"),
                Arguments.of("3.1.4-preview9-12345-extrastuff", "3.1.4"),
                Arguments.of("99.99.99-dev", "99.99.99"));
    }

    @ParameterizedTest
    @MethodSource("Versions")
    public void getVersionFromDetailedVersion(String detailedVersion, String version) {
        assertEquals(version, UserAgentHelper.getVersion(detailedVersion));
    }
}
