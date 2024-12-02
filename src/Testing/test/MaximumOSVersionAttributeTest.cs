// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class MaximumOSVersionAttributeTest
{
    [Fact]
    public void Linux_ThrowsNotImplemeneted()
    {
        Assert.Throws<NotImplementedException>(() => new MaximumOSVersionAttribute(OperatingSystems.Linux, "2.5"));
    }

    [Fact]
    public void Mac_ThrowsNotImplemeneted()
    {
        Assert.Throws<NotImplementedException>(() => new MaximumOSVersionAttribute(OperatingSystems.MacOSX, "2.5"));
    }

    [Fact]
    public void WindowsOrLinux_ThrowsNotImplemeneted()
    {
        Assert.Throws<NotImplementedException>(() => new MaximumOSVersionAttribute(OperatingSystems.Linux | OperatingSystems.Windows, "2.5"));
    }

    [Fact]
    public void DoesNotSkip_ShortVersions()
    {
        var osSkipAttribute = new MaximumOSVersionAttribute(
            OperatingSystems.Windows,
            new Version("2.5"),
            OperatingSystems.Windows,
            new Version("2.0"));

        Assert.True(osSkipAttribute.IsMet);
    }

    [Fact]
    public void DoesNotSkip_EarlierVersions()
    {
        var osSkipAttribute = new MaximumOSVersionAttribute(
            OperatingSystems.Windows,
            new Version("2.5.9"),
            OperatingSystems.Windows,
            new Version("2.0.10.12"));

        Assert.True(osSkipAttribute.IsMet);
    }

    [Fact]
    public void DoesNotSkip_SameVersion()
    {
        var osSkipAttribute = new MaximumOSVersionAttribute(
            OperatingSystems.Windows,
            new Version("2.5.10"),
            OperatingSystems.Windows,
            new Version("2.5.10.12"));

        Assert.True(osSkipAttribute.IsMet);
    }

    [Fact]
    public void Skip_LaterVersion()
    {
        var osSkipAttribute = new MaximumOSVersionAttribute(
            OperatingSystems.Windows,
            new Version("2.5.11"),
            OperatingSystems.Windows,
            new Version("3.0.10.12"));

        Assert.False(osSkipAttribute.IsMet);
    }

    [Fact]
    public void DoesNotSkip_WhenOnlyVersionsMatch()
    {
        var osSkipAttribute = new MaximumOSVersionAttribute(
            OperatingSystems.Windows,
            new Version("2.5.10.12"),
            OperatingSystems.Linux,
            new Version("2.5.10.12"));

        Assert.True(osSkipAttribute.IsMet);
    }
}
