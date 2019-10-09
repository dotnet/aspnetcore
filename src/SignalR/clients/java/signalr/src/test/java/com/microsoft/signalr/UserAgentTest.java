// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

import java.util.stream.Stream;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

public class UserAgentTest {

    private static Stream<Arguments> Versions() {
        return Stream.of(
                Arguments.of("1.0.0", "1.0"),
                Arguments.of("3.1.4-preview9-12345", "3.1"),
                Arguments.of("3.1.4-preview9-12345-extrastuff", "3.1"),
                Arguments.of("99.99.99-dev", "99.99"));
    }

    @ParameterizedTest
    @MethodSource("Versions")
    public void getVersionFromDetailedVersion(String detailedVersion, String version) {
        assertEquals(version, UserAgentHelper.getVersion(detailedVersion));
    }

    @Test
    public void verifyJavaVendor() {
        assertEquals(System.getProperty("java.vendor"), UserAgentHelper.getJavaVendor());
    }

    @Test
    public void verifyJavaVersion() {
        assertEquals(System.getProperty("java.version"), UserAgentHelper.getJavaVersion());
    }

    @Test
    public void checkUserAgentString() {
        String userAgent = UserAgentHelper.createUserAgentString();
        assertNotNull(userAgent);

        String detailedVersion = Version.getDetailedVersion();
        String handMadeUserAgent = "Microsoft SignalR/" + UserAgentHelper.getVersion(detailedVersion) +
                " (" + detailedVersion + "; " + UserAgentHelper.getOS() + "; Java; " +
                UserAgentHelper.getJavaVersion() + "; " + UserAgentHelper.getJavaVendor() + ")";

        assertEquals(handMadeUserAgent, userAgent);
    }
}
