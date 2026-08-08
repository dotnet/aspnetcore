// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorServerStateTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorServerStateTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await FeatureAppServer.StartAsync(_fixture);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [Fact]
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
