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
public partial class BlazorServerStateTests : BrowserTest
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
    public async Task ServerState_SessionUsesApplicationContractAndTempDataUsesGeneratedFrameworkContract()
    {
        await _page.GotoAsync(_server.TestUrl + "/server-state/seed");

        await Expect(_page.Locator("#session-profile")).ToHaveTextAsync("ada:36");
        await Expect(_page.Locator("#tempdata-flash")).ToHaveTextAsync("seeded");

        await _page.GotoAsync(_server.TestUrl + "/server-state?update=true");
        await _page.GotoAsync(_server.TestUrl + "/server-state");

        await Expect(_page.Locator("#session-profile")).ToHaveTextAsync("ada:37");
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
