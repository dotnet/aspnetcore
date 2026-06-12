// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestApp.E2E.Tests.Fixtures;

// One per-assembly owner of ServerFactory<E2ETestAssembly>. Test classes derive from
// Microsoft.AspNetCore.Components.Testing.Playwright.BrowserTest (or its derivatives),
// which manage the shared IPlaywright + IBrowser themselves.
[TestClass]
public static class TestRoot
{
    public static ServerFactory<E2ETestAssembly> Servers { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        Servers = new ServerFactory<E2ETestAssembly>();
        await Servers.InitializeAsync();
    }

    [AssemblyCleanup]
    public static Task Cleanup() => Servers.DisposeAsync().AsTask();
}
