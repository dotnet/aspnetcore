// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestApp.E2E.Tests.Tests;

[UITest]
public partial class LifecycleTests : UITest
{
    private bool _initialized;

    protected override Task InitializeCoreAsync()
    {
        _initialized = true;
        return Task.CompletedTask;
    }

    [TestMethod]
    public void GeneratedLifecycle_InitializesTest()
    {
        Assert.IsTrue(_initialized);
        Assert.IsNotNull(TestContext);
    }
}
