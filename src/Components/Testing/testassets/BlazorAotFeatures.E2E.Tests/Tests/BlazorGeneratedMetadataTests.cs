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
public partial class BlazorGeneratedMetadataTests : BrowserTest
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
    public async Task GeneratedMetadata_SourceGeneratedTypeInfoActivatesAndAssignsInheritedMembers()
    {
        await _page.GotoAsync(_server.TestUrl + "/generated-metadata");
        var prerenderedToken = await _page.Locator("#generated-token").TextContentAsync();
        Assert.IsFalse(string.IsNullOrEmpty(prerenderedToken));

        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#generated-increment");

        await Expect(_page.Locator("#generated-activated")).ToHaveTextAsync("yes");
        await Expect(_page.Locator("#base-parameter")).ToHaveTextAsync("base-value");
        await Expect(_page.Locator("#derived-parameter")).ToHaveTextAsync("42");
        await Expect(_page.Locator("#named-cascade")).ToHaveTextAsync("cascade-value");
        await Expect(_page.Locator("#keyed-injection")).ToHaveTextAsync("injected-greeting");
        await Expect(_page.Locator("#generated-state-phase")).ToHaveTextAsync("restored");
        await Expect(_page.Locator("#generated-token")).ToHaveTextAsync(prerenderedToken!);

        await _page.ClickAsync("#generated-increment");
        await Expect(_page.Locator("#generated-count")).ToHaveTextAsync("1");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
