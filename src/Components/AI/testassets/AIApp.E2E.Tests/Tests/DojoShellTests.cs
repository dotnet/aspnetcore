// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AIApp.Components;
using AIApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[UITest]
public partial class DojoShellTests : BrowserTest
{
    private ServerInstance _server = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_server));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task Home_RendersSevenScenarioCards()
    {
        await _page.GotoAsync(_server.TestUrl);

        var home = _page.Locator(".dojo-home");
        await Expect(home.Locator("h1")).ToHaveTextAsync("Components.AI Dojo");
        await Expect(home.Locator(".dojo-home__card")).ToHaveCountAsync(7);
        await Expect(_page.Locator(".dojo-sidebar__item")).ToHaveCountAsync(7);
    }

    [TestMethod]
    public async Task ScenarioRoute_IsInteractiveAndScopedToRequestedScenario()
    {
        await _page.GotoAsync($"{_server.TestUrl}/demo/agentic_chat");

        var demo = _page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-scenario", "agentic_chat");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");

        var scenario = demo.Locator("[data-dojo-scenario='agentic-chat']");
        await Expect(scenario).ToHaveCountAsync(1);
        await Expect(scenario.Locator(".dojo-welcome__heading"))
            .ToHaveTextAsync("How can I help you today?");
        await Expect(scenario.Locator(".sc-ai-suggestions__chip")).ToHaveCountAsync(2);
        await Expect(scenario.Locator(".sc-ai-input__textarea"))
            .ToHaveAttributeAsync("placeholder", "Type a message...");
        await Expect(demo.Locator(".dojo-scenario-placeholder")).ToHaveCountAsync(0);
    }
}
