// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal static class Constants
{
    public static readonly string UserAgent = OperatingSystem.IsBrowser() ? "X-SignalR-User-Agent" : "User-Agent";
    public static readonly string UserAgentHeader = GetUserAgentHeader();

    private static string GetUserAgentHeader()
    {
        var assemblyVersion = typeof(Constants)
            .Assembly
            .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
            .FirstOrDefault();

        Debug.Assert(assemblyVersion != null);

        var runtime = ".NET";
        var runtimeVersion = RuntimeInformation.FrameworkDescription;

        return ConstructUserAgent(typeof(Constants).Assembly.GetName().Version!, assemblyVersion.InformationalVersion, GetOS(), runtime, runtimeVersion);
    }

    private static string GetOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows NT";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macOS";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }
        else
        {
            return "";
        }
    }

    public static string ConstructUserAgent(Version version, string detailedVersion, string os, string runtime, string runtimeVersion)
    {
        var userAgent = $"Microsoft SignalR/{version.Major}.{version.Minor} (";

        if (!string.IsNullOrEmpty(detailedVersion))
        {
            userAgent += $"{detailedVersion}";
        }
        else
        {
            userAgent += "Unknown Version";
        }

        if (!string.IsNullOrEmpty(os))
        {
            userAgent += $"; {os}";
        }
        else
        {
            userAgent += "; Unknown OS";
        }

        userAgent += $"; {runtime}";

        if (!string.IsNullOrEmpty(runtimeVersion))
        {
            userAgent += $"; {runtimeVersion}";
        }
        else
        {
            userAgent += "; Unknown Runtime Version";
        }

        userAgent += ")";

        return userAgent;
    }
}
