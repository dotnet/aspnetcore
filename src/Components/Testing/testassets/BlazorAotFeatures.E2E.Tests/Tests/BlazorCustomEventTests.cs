// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorAotFeatures.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace BlazorAotFeatures.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class BlazorCustomEventTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private IPage _page = null!;
    private ServerInstance _server = null!;

    public BlazorCustomEventTests(ServerFixture<E2ETestAssembly> fixture)
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
