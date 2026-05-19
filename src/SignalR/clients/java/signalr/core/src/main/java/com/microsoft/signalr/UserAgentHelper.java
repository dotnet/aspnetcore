// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        agentBuilder.append("; ");
        if (!os.isEmpty()) {
            agentBuilder.append(os);
        } else {
            agentBuilder.append("Unknown OS");
        }

        agentBuilder.append("; ");
        agentBuilder.append(runtime);

        agentBuilder.append("; ");
        if (!runtimeVersion.isEmpty()) {
            agentBuilder.append(runtimeVersion);
        } else {
            agentBuilder.append("Unknown Runtime Version");
        }

        agentBuilder.append("; ");
        if (!vendor.isEmpty()) {
            agentBuilder.append(vendor);
        } else {
            agentBuilder.append("Unknown Vendor");
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
