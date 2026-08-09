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
        "Develop a comprehensive mission plan, detailing objectives, budget, and timeline.",
        "Design and test a spacecraft capable of transporting humans and cargo to Mars.",
        "Select and train astronaut crew for the mission.",
        "Establish communication systems and infrastructure for Mars exploration.",
        "Launch the spacecraft and execute the mission to Mars.",
    ];

    [TestMethod]
    public async Task PlanTask_StreamsProgressAndCompletesAfterFinalResponse()
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
        var activity = scenario.Locator(".plan-activity");
        var checkpoint = demo.Locator(".replay-checkpoint-status");
        var simple = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Simple plan", Exact = true });
        var complex = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Complex plan", Exact = true });

        await Expect(activity).ToHaveCountAsync(0);
        await Expect(simple).ToBeEnabledAsync();
        await Expect(complex).ToBeEnabledAsync();

        var frames = Enumerable.Range(0, 6)
            .Select(index => session.Lock(script.GetLockName(0, index)))
            .ToArray();
        await using (frames[0])
        await using (frames[1])
        await using (frames[2])
        await using (frames[3])
        await using (frames[4])
        await using (frames[5])
        {
            await simple.ClickAsync();

            var turn = scenario.Locator(".sc-ai-turn");
            await Expect(turn).ToHaveCountAsync(1);
            await Expect(turn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Please build a plan to go to mars in 5 steps.");
            await AssertPlanAsync(activity, completedCount: 0, isRunning: true);
            await Expect(send).ToBeDisabledAsync();
            await Expect(simple).ToBeDisabledAsync();
            await Expect(complex).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "plan-created");

            for (var completedCount = 1; completedCount < s_stepDescriptions.Length; completedCount++)
            {
                await frames[completedCount - 1].ReleaseAsync();
                await AssertPlanAsync(activity, completedCount, isRunning: true);
                await Expect(send).ToBeDisabledAsync();
                await AssertCheckpointAsync(checkpoint, $"step-{completedCount}-complete");
            }

            await frames[4].ReleaseAsync();

            await AssertPlanAsync(activity, completedCount: 5, isRunning: true);
            var assistant = turn.Last.Locator(
                ".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(assistant).ToHaveTextAsync(
                "All five steps in the Mars mission plan are complete.");
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "plan-complete");

            await frames[5].ReleaseAsync();

            await AssertPlanAsync(activity, completedCount: 5, isRunning: false);
            await Expect(assistant).ToHaveClassAsync("sc-ai-message__content");
            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(simple).ToBeEnabledAsync();
            await Expect(complex).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }
    }

    private static async Task AssertPlanAsync(
        ILocator activity,
        int completedCount,
        bool isRunning)
    {
        await Expect(activity).ToHaveCountAsync(1);
        await Expect(activity).ToHaveAttributeAsync(
            "data-task-status",
            isRunning ? "running" : "done");
        await Expect(activity.Locator(".plan-activity__status"))
            .ToHaveTextAsync(isRunning ? "Running" : "Done");

        var card = activity.Locator(".plan-progress-card");
        await Expect(card.Locator(".plan-progress-card__header h3"))
            .ToHaveTextAsync("Task Progress");
        await Expect(card.Locator(".plan-progress-card__count"))
            .ToHaveTextAsync($"{completedCount}/{s_stepDescriptions.Length} Complete");
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
                    ? "current"
                    : "pending";
            await Expect(step).ToHaveAttributeAsync("data-step-state", expectedState);
            await Expect(step).ToHaveClassAsync($"plan-step plan-step--{expectedState}");
            await Expect(step.Locator(".plan-step__processing"))
                .ToHaveCountAsync(expectedState == "current" ? 1 : 0);
        }

        await Expect(card.Locator(".plan-step__processing"))
            .ToHaveCountAsync(completedCount < s_stepDescriptions.Length ? 1 : 0);
    }

    private static async Task AssertCheckpointAsync(ILocator checkpoint, string name)
    {
        await Expect(checkpoint).ToHaveAttributeAsync("data-replay-checkpoint", name);
    }
}
