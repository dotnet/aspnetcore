// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
public class MaximumOSVersionTest
{
    [ConditionalFact]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
    public void RunTest_Win7DoesRunOnWin7()
    {
        Assert.True(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
            "Test should only be running on Win7 or Win2008R2.");
    }

    [ConditionalTheory]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
    [InlineData(1)]
    public void RunTheory_Win7DoesRunOnWin7(int arg)
    {
        Assert.True(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
            "Test should only be running on Win7 or Win2008R2.");
    }

    [ConditionalFact]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public void RunTest_Win10_RS4()
    {
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        Assert.NotNull(versionKey);
        var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
        Assert.NotNull(currentVersion);
        Assert.True(17134 >= int.Parse(currentVersion, CultureInfo.InvariantCulture));
    }

    [ConditionalFact]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public void RunTest_Win10_19H2()
    {
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        Assert.NotNull(versionKey);
        var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
        Assert.NotNull(currentVersion);
        Assert.True(18363 >= int.Parse(currentVersion, CultureInfo.InvariantCulture));
    }
}

[MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
public class OSMaxVersionClassTest
{
    [ConditionalFact]
    public void TestSkipClass_Win7DoesRunOnWin7()
    {
        Assert.True(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
            "Test should only be running on Win7 or Win2008R2.");
    }
}

// Let this one run cross plat just to check the constructor logic.
[MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
public class OSMaxVersionCrossPlatTest
{
    [ConditionalFact]
    public void TestSkipClass_Win7DoesRunOnWin7()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.True(Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
                "Test should only be running on Win7 or Win2008R2.");
        }
    }
}
