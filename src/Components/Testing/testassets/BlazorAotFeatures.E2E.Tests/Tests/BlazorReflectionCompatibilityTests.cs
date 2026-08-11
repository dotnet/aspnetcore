// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using BlazorServerAotSample;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[UITest]
public partial class BlazorReflectionCompatibilityTests : BrowserTest
{
    private IPage _page = null!;
    private ServerInstance _server = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<FeatureApp>(TestRoot.Servers, FeatureAppServer.Configure);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    [TestCategory("JitDefault")]
    public async Task JitDefaults_ReflectionFallbacksRemainCompatible()
    {
        await _page.GotoAsync(_server.TestUrl + "/reflection-fallbacks");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#trigger-component-fallback");

        await _page.ClickAsync("#trigger-component-fallback");
        await Expect(_page.Locator("#component-fallback-success")).ToHaveTextAsync("component-reflection-ok");
        await Expect(_page.Locator("#component-fallback-error")).ToHaveCountAsync(0);

        await _page.ClickAsync("#trigger-js-fallback");
        await Expect(_page.Locator("#js-fallback-success")).ToHaveTextAsync("js-reflection-ok");
        await Expect(_page.Locator("#js-fallback-error")).ToHaveTextAsync("");

        await _page.ClickAsync("#trigger-json-fallback");
        await Expect(_page.Locator("#json-fallback-success")).ToHaveTextAsync("json-reflection-ok");
        await Expect(_page.Locator("#json-fallback-error")).ToHaveTextAsync("");
    }
}
