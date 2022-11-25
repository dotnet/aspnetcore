// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETests.Playwright.Infrastructure.Adapters;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

public abstract class BrowserAdapterTestBase : BrowserTestBase, IClassFixture<BrowserFixture>
{
    public BrowserAdapterTestBase(ITestOutputHelper output) : base(output)
    {
    }

    protected override async Task InitializeCoreAsync(TestContext context)
    {
        await base.InitializeCoreAsync(context);
        var browser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
        Browser = new BrowserAdapter(browser);
        InitializeAsyncCore();
    }

    public override async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        await base.DisposeAsync();
    }

    protected virtual void InitializeAsyncCore()
    {
    }

    public BrowserAdapter Browser { get; private set; }
}
