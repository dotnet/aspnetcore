// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class OSSkipConditionTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    public void TestSkipLinux()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "Test should not be running on Linux");
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public void TestSkipMacOSX()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            "Test should not be running on MacOSX.");
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows)]
    public void TestSkipWindows()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Test should not be running on Windows.");
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public void TestSkipLinuxAndMacOSX()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "Test should not be running on Linux.");
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            "Test should not be running on MacOSX.");
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux)]
    [InlineData(1)]
    public void TestTheorySkipLinux(int arg)
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "Test should not be running on Linux");
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    [InlineData(1)]
    public void TestTheorySkipMacOS(int arg)
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            "Test should not be running on MacOSX.");
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Windows)]
    [InlineData(1)]
    public void TestTheorySkipWindows(int arg)
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Test should not be running on Windows.");
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    [InlineData(1)]
    public void TestTheorySkipLinuxAndMacOSX(int arg)
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            "Test should not be running on Linux.");
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            "Test should not be running on MacOSX.");
    }
}

[OSSkipCondition(OperatingSystems.Windows)]
public class OSSkipConditionClassTest
{
    [ConditionalFact]
    public void TestSkipClassWindows()
    {
        Assert.False(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "Test should not be running on Windows.");
    }
}
