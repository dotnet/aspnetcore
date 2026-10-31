// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class MinimumOSVersionTest
{
    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
    public void RunTest_Win8DoesNotRunOnWin7()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
            "Test should not be running on Win7 or Win2008R2.");
    }

    [ConditionalTheory]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
    [InlineData(1)]
    public void RunTheory_Win8DoesNotRunOnWin7(int arg)
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
            "Test should not be running on Win7 or Win2008R2.");
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public void RunTest_Win10_RS4()
    {
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        Assert.NotNull(versionKey);
        var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
        Assert.NotNull(currentVersion);
        Assert.True(17134 <= int.Parse(currentVersion, CultureInfo.InvariantCulture));
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public void RunTest_Win10_19H2()
    {
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        Assert.NotNull(versionKey);
        var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
        Assert.NotNull(currentVersion);
        Assert.True(18363 <= int.Parse(currentVersion, CultureInfo.InvariantCulture));
    }
}

[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
public class OSMinVersionClassTest
{
    [ConditionalFact]
    public void TestSkipClass_Win8DoesNotRunOnWin7()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
            "Test should not be running on Win7 or Win2008R2.");
    }
}
