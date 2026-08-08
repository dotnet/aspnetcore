// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorDynamicRootTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorDynamicRootTests(ServerFixture<E2ETestAssembly> fixture)
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
    public async Task DynamicRoots_AddUpdateAndRemoveThroughGeneratedMetadata()
    {
        await _page.GotoAsync(_server.TestUrl + "/dynamic-roots");
        await _page.WaitForBlazorAsync();
        await _page.WaitForInteractiveAsync("#add-dynamic-root");

        await _page.ClickAsync("#add-dynamic-root");
        await Expect(_page.Locator("#dynamic-root-status")).ToHaveTextAsync("added");
        await Expect(_page.Locator("#dynamic-root-label")).ToHaveTextAsync("first");
        await Expect(_page.Locator("#dynamic-root-step")).ToHaveTextAsync("1");
        await Expect(_page.Locator("#dynamic-root-injected")).ToHaveTextAsync("injected-greeting");

        await _page.ClickAsync("#dynamic-root-increment");
        await Expect(_page.Locator("#dynamic-root-count")).ToHaveTextAsync("1");

        await _page.ClickAsync("#update-dynamic-root");
        await Expect(_page.Locator("#dynamic-root-status")).ToHaveTextAsync("updated");
        await Expect(_page.Locator("#dynamic-root-label")).ToHaveTextAsync("updated");
        await Expect(_page.Locator("#dynamic-root-step")).ToHaveTextAsync("4");

        await _page.ClickAsync("#remove-dynamic-root");
        await Expect(_page.Locator("#dynamic-root-status")).ToHaveTextAsync("disposed");
        await Expect(_page.Locator("#dynamic-root-label")).ToHaveCountAsync(0);
        await Expect(_page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
