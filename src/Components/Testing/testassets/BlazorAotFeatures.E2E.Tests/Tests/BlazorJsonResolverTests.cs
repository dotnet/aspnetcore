// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorJsonResolverTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorJsonResolverTests(ServerFixture<E2ETestAssembly> fixture)
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
