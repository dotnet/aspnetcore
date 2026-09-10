// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class DojoBackendConfigurationTests
{
    [TestMethod]
    public void Selection_PreservesDefaultAndRequiresExactBackendNames()
    {
        Assert.AreEqual(DojoBackendKind.AGUI, DojoBackendConfiguration.Parse(null));
        Assert.AreEqual(DojoBackendKind.AGUI, DojoBackendConfiguration.Parse("AGUI"));
        Assert.AreEqual(DojoBackendKind.Direct, DojoBackendConfiguration.Parse("Direct"));
        foreach (var value in new[] { "", "agui", "direct", "0", "1", "unknown" })
        {
            Assert.Throws<InvalidOperationException>(() => DojoBackendConfiguration.Parse(value));
        }
    }
}
