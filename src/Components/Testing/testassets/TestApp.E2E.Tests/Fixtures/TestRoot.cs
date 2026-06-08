// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestApp.E2E.Tests.Fixtures;

// Single owner of the per-assembly ServerFactory and the per-assembly Playwright/IBrowser.
// Populated from [AssemblyInitialize] and disposed from [AssemblyCleanup]. Reached for
// directly by every test class via TestRoot.Servers / TestRoot.Browser.
[TestClass]
public static class TestRoot
{
    public static ServerFactory<E2ETestAssembly> Servers { get; private set; } = null!;
    public static IPlaywright Playwright { get; private set; } = null!;
    public static IBrowser Browser { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task Init(TestContext _)
    {
        Servers = new ServerFactory<E2ETestAssembly>();
        await Servers.InitializeAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync();
    }

    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }
        Playwright?.Dispose();
        if (Servers is not null)
        {
            await Servers.DisposeAsync();
        }
    }
}
