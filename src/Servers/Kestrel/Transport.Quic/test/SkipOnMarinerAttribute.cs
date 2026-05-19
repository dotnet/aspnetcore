// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Tests;

[AttributeUsage(AttributeTargets.Method)]
public class SkipOnMarinerAttribute : Attribute, ITestCondition
{
    public SkipOnMarinerAttribute(string issueUrl = "")
    {
        IssueUrl = issueUrl;
    }

    public string IssueUrl { get; }

    public bool IsMet { get; } = !IsMariner;

    public string SkipReason => "Test cannot run on Mariner Linux.";

    // This logic is borrowed from https://github.com/dotnet/runtime/blob/6a5a78bec9a6e14b4aa52cd5ac558f6cf5c6a211/src/libraries/Common/tests/TestUtilities/System/PlatformDetection.Unix.cs
    private static bool IsMariner { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/etc/os-release") &&
        File.ReadAllLines("/etc/os-release").Any(line =>
            line.StartsWith("ID=", StringComparison.Ordinal) && line.Substring(3).Trim('"', '\'') == "mariner");
}
