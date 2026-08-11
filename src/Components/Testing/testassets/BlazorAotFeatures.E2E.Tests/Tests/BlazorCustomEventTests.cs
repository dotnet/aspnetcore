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
public partial class BlazorCustomEventTests : BrowserTest
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
    public async Task CustomEvents_DeserializeApplicationArgsThroughUnifiedResolver()
    {
        await _page.GotoAsync(_server.TestUrl + "/custom-events");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#register-aot-event");

        await _page.ClickAsync("#register-aot-event");
        await Expect(_page.Locator("#event-registration-status")).ToHaveTextAsync("registered");
        await _page.ClickAsync("#dispatch-aot-event");

        await Expect(_page.Locator("#custom-event-message")).ToHaveTextAsync("hello");
        await Expect(_page.Locator("#custom-event-payload")).ToHaveTextAsync("cat:milo");
        await Expect(_page.Locator("#custom-event-count")).ToHaveTextAsync("1");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
