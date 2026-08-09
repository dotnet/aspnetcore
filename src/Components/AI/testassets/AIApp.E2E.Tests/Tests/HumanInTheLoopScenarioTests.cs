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
public partial class HumanInTheLoopScenarioTests : BrowserTest
{
    [TestMethod]
    public async Task TaskSteps_UsesHumanSelectionBeforeCompletingStream()
    {
        var script = ReplayCheckpointScript.Load("Dojo_HumanInTheLoop.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.HumanInTheLoop));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/human_in_the_loop"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='human-in-the-loop']");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");
        var checkpoint = demo.Locator(".replay-checkpoint-status");
        var taskStepsReview = session.Lock(script.GetLockName(0, 0));
        var summaryStart = session.Lock(script.GetLockName(1, 0));
        var summaryFinal = session.Lock(script.GetLockName(1, 1));
        await using (taskStepsReview)
        await using (summaryStart)
        await using (summaryFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Simple plan", Exact = true }).ClickAsync();

            var turn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(1);
            await Expect(turn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync(
                    "Help me organize a birthday party for my friend next Saturday. " +
                    "Generate the task steps I need to complete.");
            var card = turn.Locator(".task-steps-card");
            await Expect(card).ToHaveCountAsync(1);
            await Expect(card.Locator("h3")).ToHaveTextAsync("Select Steps");
            await Expect(card.Locator(".task-steps-card__count")).ToHaveTextAsync("2 / 3 selected");
            var steps = card.Locator(".task-step-item");
            await Expect(steps).ToHaveCountAsync(3);
            await Expect(steps.Nth(0)).ToHaveTextAsync("Book a party venue");
            await Expect(steps.Nth(1)).ToHaveTextAsync("Order a birthday cake");
            await Expect(steps.Nth(2)).ToHaveTextAsync("Send invitations");
            await Expect(steps.Nth(0)).ToHaveClassAsync("task-step-item task-step-item--selected");
            await Expect(steps.Nth(1)).ToHaveClassAsync("task-step-item task-step-item--selected");
            await Expect(steps.Nth(2)).ToHaveClassAsync("task-step-item ");
            var checkboxes = card.GetByRole(AriaRole.Checkbox);
            await Expect(checkboxes).ToHaveCountAsync(3);
            await Expect(checkboxes.Nth(0)).ToBeCheckedAsync();
            await Expect(checkboxes.Nth(1)).ToBeCheckedAsync();
            await Expect(checkboxes.Nth(2)).Not.ToBeCheckedAsync();
            await Expect(card.GetByRole(
                AriaRole.Button,
                new() { Name = "Reject", Exact = true })).ToBeEnabledAsync();
            await Expect(card.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true })).ToBeEnabledAsync();
            await Expect(send).ToBeDisabledAsync();
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "task-steps-review");

            await checkboxes.Nth(1).ClickAsync();
            await checkboxes.Nth(2).ClickAsync();

            await Expect(card.Locator(".task-steps-card__count")).ToHaveTextAsync("2 / 3 selected");
            await Expect(steps.Nth(0)).ToHaveClassAsync("task-step-item task-step-item--selected");
            await Expect(steps.Nth(1)).ToHaveClassAsync("task-step-item ");
            await Expect(steps.Nth(2)).ToHaveClassAsync("task-step-item task-step-item--selected");
            await Expect(checkboxes.Nth(0)).ToBeCheckedAsync();
            await Expect(checkboxes.Nth(1)).Not.ToBeCheckedAsync();
            await Expect(checkboxes.Nth(2)).ToBeCheckedAsync();
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "task-steps-review");

            await taskStepsReview.ReleaseAsync();
            await card.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true }).ClickAsync();

            await Expect(card).ToHaveClassAsync("task-steps-card task-steps-card--responded");
            await Expect(card).ToHaveTextAsync("\u2713 Accepted");
            await Expect(card.Locator(".task-steps-status--accepted")).ToHaveCountAsync(1);
            await Expect(card.GetByRole(AriaRole.Checkbox)).ToHaveCountAsync(0);
            var assistant = turn.Locator(".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(assistant)
                .ToHaveTextAsync("I'll move forward with booking a party venue");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "selection-summary-start");

            await summaryStart.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync(
                "I'll move forward with booking a party venue and sending invitations.");
            await Expect(assistant).ToHaveClassAsync(
                "sc-ai-message__content sc-ai-message__content--streaming");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "selection-summary-final");

            await summaryFinal.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync(
                "I'll move forward with booking a party venue and sending invitations.");
            await Expect(assistant).ToHaveClassAsync("sc-ai-message__content");
            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }
    }
}
