// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AIApp.Components;
using AIApp.E2E.Tests.Fixtures;
using AIApp.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[UITest]
public partial class BackendToolRenderingScenarioTests : BrowserTest
{
    [TestMethod]
    public async Task WeatherTool_ReplaysCardAndSummaryFramesBeforeCompletingStream()
    {
        var script = ReplayCheckpointScript.Load("Dojo_BackendToolRendering.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.BackendToolRendering));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/backend_tool_rendering"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='backend-tool-rendering']");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");
        var toolCall = session.Lock(script.GetLockName(0, 0));
        var summaryStart = session.Lock(script.GetLockName(1, 0));
        var summaryFinal = session.Lock(script.GetLockName(1, 1));
        await using (toolCall)
        await using (summaryStart)
        await using (summaryFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Weather in San Francisco", Exact = true }).ClickAsync();

            var turn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(1);
            await Expect(turn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("What is the weather in San Francisco?");
            await Expect(turn.Locator(".weather-card")).ToHaveCountAsync(1);
            await Expect(turn.Locator(".weather-card--loading"))
                .ToHaveTextAsync("\u2699\ufe0f Retrieving weather...");
            await Expect(send).ToBeDisabledAsync();

            await toolCall.ReleaseAsync();

            var card = turn.Locator(".weather-card");
            await Expect(turn.Locator(".weather-card--loading")).ToHaveCountAsync(0);
            await Expect(card).ToHaveCountAsync(1);
            await Expect(card.Locator(".weather-card__location")).ToHaveTextAsync("San Francisco");
            await Expect(card.Locator(".weather-card__temp-value")).ToHaveTextAsync("20");
            await Expect(card.Locator(".weather-card__temp-unit")).ToHaveTextAsync("\u00b0C");
            await Expect(card.Locator(".weather-card__condition")).ToHaveTextAsync("sunny");
            await Expect(card.Locator(".weather-card__stat")).ToHaveCountAsync(3);
            await Expect(card.Locator(".weather-card__stat-value").Nth(0)).ToHaveTextAsync("50%");
            await Expect(card.Locator(".weather-card__stat-value").Nth(1)).ToHaveTextAsync("10 km/h");
            await Expect(card.Locator(".weather-card__stat-value").Nth(2)).ToHaveTextAsync("25\u00b0C");

            var assistant = turn.Locator(".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(assistant).ToHaveTextAsync("The weather in San Francisco is sunny");

            await summaryStart.ReleaseAsync();

            await Expect(assistant)
                .ToHaveTextAsync("The weather in San Francisco is sunny with a temperature of 20\u00b0C.");
            await Expect(assistant).ToHaveClassAsync(
                "sc-ai-message__content sc-ai-message__content--streaming");
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();

            await summaryFinal.ReleaseAsync();

            await Expect(assistant)
                .ToHaveTextAsync("The weather in San Francisco is sunny with a temperature of 20\u00b0C.");
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }
    }
}
