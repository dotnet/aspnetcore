// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests.Infrastructure;

[TestClass]
[TestCategory("Infrastructure")]
public class DojoBackendConfigurationTests
{
    [TestMethod]
    public void Selection_PreservesDefaultAndRequiresExactBackendNames()
    {
        Assert.AreEqual(DojoBackendKind.AGUI, DojoBackendConfiguration.Parse(null));
        Assert.AreEqual(DojoBackendKind.AGUI, DojoBackendConfiguration.Parse("AGUI"));
        Assert.AreEqual(DojoBackendKind.Direct, DojoBackendConfiguration.Parse("Direct"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("agui")]
    [DataRow("direct")]
    [DataRow("0")]
    [DataRow("1")]
    [DataRow("unknown")]
    public void Selection_RejectsInvalidBackendNames(string value)
    {
        Assert.Throws<InvalidOperationException>(() => DojoBackendConfiguration.Parse(value));
    }
}
