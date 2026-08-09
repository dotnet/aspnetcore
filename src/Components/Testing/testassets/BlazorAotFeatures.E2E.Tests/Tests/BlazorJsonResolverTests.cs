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
public partial class BlazorJsonResolverTests : BrowserTest
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
    public async Task JsonResolvers_PartialAndMultipleContextsComposeInRegistrationOrder()
    {
        await _page.GotoAsync(_server.TestUrl + "/json-resolver-composition");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#exercise-json-resolvers");
        await _page.ClickAsync("#exercise-json-resolvers");

        await Expect(_page.Locator("#json-partial-a")).ToHaveTextAsync("a");
        await Expect(_page.Locator("#json-partial-b")).ToHaveTextAsync("b");
        await Expect(_page.Locator("#json-interop-only")).ToHaveTextAsync("interop");
        await Expect(_page.Locator("#json-storage-only")).ToHaveTextAsync("storage");
        await Expect(_page.Locator("#json-precedence-wire")).ToHaveTextAsync("""{"someValue":"first"}""");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
