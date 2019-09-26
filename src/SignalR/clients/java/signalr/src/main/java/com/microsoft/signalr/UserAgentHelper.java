// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

public class UserAgentHelper {

    private final static String USER_AGENT = "User-Agent";

    public static String getUserAgentName() {
        return USER_AGENT;
    }

    public static String createUserAgentString() {
        return constructUserAgentString(Version.getDetailedVersion(), getOS(), "Java", getJavaVersion(), getJavaVendor());
    }

    public static String constructUserAgentString(String detailedVersion, String os, String runtime, String runtimeVersion, String vendor) {
        StringBuilder agentBuilder = new StringBuilder("Microsoft SignalR/");

        agentBuilder.append(getVersion(detailedVersion));
        agentBuilder.append(" (");
        agentBuilder.append(detailedVersion);

        if (!os.isEmpty()) {
            agentBuilder.append("; ");
            agentBuilder.append(os);
        }

        if (!runtime.isEmpty()) {
            agentBuilder.append("; ");
            agentBuilder.append(runtime);
        }

        if (!runtimeVersion.isEmpty()) {
            agentBuilder.append("; ");
            agentBuilder.append(runtimeVersion);
        }

        if (!vendor.isEmpty()) {
            agentBuilder.append("; ");
            agentBuilder.append(vendor);
        }

        agentBuilder.append(")");

        return agentBuilder.toString();
    }

    static String getVersion(String detailedVersion) {
        // Getting the index of the second . so we can return just the major and minor version.
        int shortVersionIndex = detailedVersion.indexOf(".", detailedVersion.indexOf(".") + 1);
        return detailedVersion.substring(0, shortVersionIndex);
    }

    static String getJavaVendor() {
        return System.getProperty("java.vendor");
    }

    static String getJavaVersion() {
        return System.getProperty("java.version");
    }

    static String getOS() {
        String osName = System.getProperty("os.name").toLowerCase();

        if (osName.indexOf("win") >= 0) {
            return "Windows NT";
        } else if (osName.contains("mac") || osName.contains("darwin")) {
            return "macOS";
        } else if (osName.contains("linux")) {
            return "Linux";
        } else {
            return osName;
        }
    }
}
