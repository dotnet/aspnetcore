// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting.Tests;

public class QuarantinedTestAttributeTest
{
    [Fact]
    public void QuarantinedTest_AppliesToAllOperatingSystemsByDefault()
    {
        Assert.True(QuarantinedTestTraitDiscoverer.IsQuarantined(
            new QuarantinedTestAttribute("reason"),
            OperatingSystems.Windows));
    }

    [Fact]
    public void QuarantinedTest_AppliesToSelectedOperatingSystems()
    {
        var attribute = new QuarantinedTestAttribute("reason", OperatingSystems.Linux);

        Assert.True(QuarantinedTestTraitDiscoverer.IsQuarantined(attribute, OperatingSystems.Linux));
        Assert.False(QuarantinedTestTraitDiscoverer.IsQuarantined(attribute, OperatingSystems.Windows));
    }

    [Fact]
    public void QuarantinedTestData_AddsTraitOnlyOnSelectedOperatingSystems()
    {
        var traits = new Dictionary<string, List<string>>();
        var currentOperatingSystem = TestPlatformHelper.IsWindows
            ? OperatingSystems.Windows
            : TestPlatformHelper.IsLinux
                ? OperatingSystems.Linux
                : OperatingSystems.MacOSX;

        ConditionalTheoryDiscoverer.AddQuarantinedTrait(
            traits,
            new QuarantinedTestAttribute("reason", currentOperatingSystem));

        Assert.Equal(["true"], traits["Quarantined"]);

        traits.Clear();
        var otherOperatingSystem = currentOperatingSystem == OperatingSystems.Windows
            ? OperatingSystems.Linux
            : OperatingSystems.Windows;

        ConditionalTheoryDiscoverer.AddQuarantinedTrait(
            traits,
            new QuarantinedTestAttribute("reason", otherOperatingSystem));

        Assert.Empty(traits);
    }

    [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
    [QuarantinedTest("No issue")]
    public void AlwaysFlakyInCI()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
        {
            throw new Exception("Flaky!");
        }
    }

    [Fact]
    [QuarantinedTest("No issue, used to verify retry is working")]
    public void FlakyTestToEnsureRetryWorks()
    {
        // Fail 20% of the time
        Assert.True(new Random().Next(100) <= 80);
    }
}
