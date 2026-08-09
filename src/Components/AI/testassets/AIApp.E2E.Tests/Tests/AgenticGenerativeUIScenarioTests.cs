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
public partial class AgenticGenerativeUIScenarioTests : BrowserTest
{
    private static readonly string[] s_stepDescriptions =
    [
        "Gather ingredients",
        "Mix dough",
        "Let it rise",
        "Shape loaves",
        "Bake",
    ];

    [TestMethod]
    public async Task PlanProgress_StreamsEveryStateBeforeCompleting()
    {
        var script = ReplayCheckpointScript.Load("Dojo_AgenticGenerativeUI.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.AgenticGenerativeUI));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/agentic_generative_ui"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='agentic-generative-ui']");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");
        var card = scenario.Locator(".plan-progress-card");
        var checkpoint = demo.Locator(".replay-checkpoint-status");

        await Expect(card).ToHaveCountAsync(0);

        var planCreated = session.Lock(script.GetLockName(0, 0));
        var ingredientsComplete = session.Lock(script.GetLockName(0, 1));
        var mixingComplete = session.Lock(script.GetLockName(0, 2));
        var risingComplete = session.Lock(script.GetLockName(0, 3));
        var shapingComplete = session.Lock(script.GetLockName(0, 4));
        var planComplete = session.Lock(script.GetLockName(0, 5));
        await using (planCreated)
        await using (ingredientsComplete)
        await using (mixingComplete)
        await using (risingComplete)
        await using (shapingComplete)
        await using (planComplete)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Simple plan", Exact = true }).ClickAsync();

            var turn = scenario.Locator(".sc-ai-turn");
            await Expect(turn).ToHaveCountAsync(1);
            await Expect(turn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Create a plan for learning to bake bread");
            await AssertPlanAsync(card, 0);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "plan-created");

            await planCreated.ReleaseAsync();

            await AssertPlanAsync(card, 1);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "ingredients-complete");

            await ingredientsComplete.ReleaseAsync();

            await AssertPlanAsync(card, 2);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "mixing-complete");

            await mixingComplete.ReleaseAsync();

            await AssertPlanAsync(card, 3);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "rising-complete");

            await risingComplete.ReleaseAsync();

            await AssertPlanAsync(card, 4);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "shaping-complete");

            await shapingComplete.ReleaseAsync();

            await AssertPlanAsync(card, 5);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "plan-complete");

            await planComplete.ReleaseAsync();

            await AssertPlanAsync(card, 5);
            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }
    }

    private static async Task AssertPlanAsync(ILocator card, int completedCount)
    {
        await Expect(card).ToHaveCountAsync(1);
        await Expect(card).ToHaveClassAsync("plan-progress-card");
        await Expect(card.Locator(".plan-progress-card__header h3"))
            .ToHaveTextAsync("Task Progress");
        await Expect(card.Locator(".plan-progress-card__count"))
            .ToHaveTextAsync($"{completedCount} / {s_stepDescriptions.Length}");
        await Expect(card.Locator(".plan-progress-card__bar"))
            .ToHaveClassAsync("plan-progress-card__bar");
        await Expect(card.Locator(".plan-progress-card__bar-fill"))
            .ToHaveClassAsync("plan-progress-card__bar-fill");
        await Expect(card.Locator(".plan-progress-card__bar-fill"))
            .ToHaveAttributeAsync("style", $"width: {completedCount * 20}%");

        var steps = card.Locator(".plan-step");
        await Expect(steps).ToHaveCountAsync(s_stepDescriptions.Length);
        for (var index = 0; index < s_stepDescriptions.Length; index++)
        {
            var step = steps.Nth(index);
            await Expect(step.Locator(".plan-step__description"))
                .ToHaveTextAsync(s_stepDescriptions[index]);

            var expectedState = index < completedCount
                ? "completed"
                : completedCount < s_stepDescriptions.Length && index == completedCount
                    ? "executing"
                    : "pending";
            await Expect(step).ToHaveClassAsync($"plan-step plan-step--{expectedState}");
            await Expect(step.Locator(".plan-step__icon")).ToHaveTextAsync(expectedState switch
            {
                "completed" => "\u2713",
                "executing" => "\u27f3",
                _ => "\u23f0",
            });
        }
    }

    private static async Task AssertCheckpointAsync(ILocator checkpoint, string name)
    {
        await Expect(checkpoint).ToHaveAttributeAsync("data-replay-checkpoint", name);
    }
}
