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
public partial class AgenticChatScenarioTests : BrowserTest
{
    [TestMethod]
    public async Task ChangeBackground_ReplaysExactFramesBeforeCompletingStream()
    {
        var script = ReplayCheckpointScript.Load("Dojo_AgenticChat.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(nameof(ChatClientOverrides.AgenticChat));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/agentic_chat"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='agentic-chat']");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");
        var reset = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Reset", Exact = true });
        var checkpoint = demo.Locator(".replay-checkpoint-status");
        var actionFrame = session.Lock(script.GetLockName(0, 0));
        var confirmationStart = session.Lock(script.GetLockName(1, 0));
        var confirmationFinal = session.Lock(script.GetLockName(1, 1));
        await using (actionFrame)
        await using (confirmationStart)
        await using (confirmationFinal)
        {
            await scenario.GetByRole(AriaRole.Button, new() { Name = "Change background", Exact = true }).ClickAsync();

            var turn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(turn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Change the background to something new");
            await Expect(scenario).ToHaveAttributeAsync(
                "style",
                "background: linear-gradient(135deg, #ff9a9e, #fad0c4);");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "background-action");
            await Expect(reset).ToBeDisabledAsync();
            await reset.EvaluateAsync("button => button.click()");
            await Expect(scenario).ToHaveAttributeAsync(
                "style",
                "background: linear-gradient(135deg, #ff9a9e, #fad0c4);");
            await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(1);
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "background-action");

            await actionFrame.ReleaseAsync();

            var assistant = turn.Locator(".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(assistant).ToHaveTextAsync("Background changed");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-start");

            await confirmationStart.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync("Background changed to a sunset gradient.");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-final");

            await confirmationFinal.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync("Background changed to a sunset gradient.");
            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(reset).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");

            await reset.ClickAsync();

            await Expect(scenario).ToHaveAttributeAsync("style", "");
            await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(0);
            await Expect(scenario.Locator(".dojo-welcome__heading"))
                .ToHaveTextAsync("How can I help you today?");
        }

        session.ResetReplay();
        var resetActionFrame = session.Lock(script.GetLockName(0, 0));
        var resetConfirmationStart = session.Lock(script.GetLockName(1, 0));
        var resetConfirmationFinal = session.Lock(script.GetLockName(1, 1));
        await using (resetActionFrame)
        await using (resetConfirmationStart)
        await using (resetConfirmationFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Change background", Exact = true }).ClickAsync();

            var resetTurn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(resetTurn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Change the background to something new");
            await Expect(scenario).ToHaveAttributeAsync(
                "style",
                "background: linear-gradient(135deg, #ff9a9e, #fad0c4);");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "background-action");

            await resetActionFrame.ReleaseAsync();

            var resetAssistant = resetTurn.Locator(
                ".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(resetAssistant).ToHaveTextAsync("Background changed");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-start");

            await resetConfirmationStart.ReleaseAsync();

            await Expect(resetAssistant)
                .ToHaveTextAsync("Background changed to a sunset gradient.");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-final");

            await resetConfirmationFinal.ReleaseAsync();

            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
        }
    }

    [TestMethod]
    public async Task TwoCircuits_ReleaseOnlyTheirOwnCallCheckpoints()
    {
        var script = ReplayCheckpointScript.Load("Dojo_AgenticChat.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(nameof(ChatClientOverrides.AgenticChat));
        });
        var firstContext = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var secondContext = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var firstSession = await ReplayTestSession.CreateAsync(server, firstContext);
        var secondSession = await ReplayTestSession.CreateAsync(server, secondContext);
        var firstPage = await firstContext.NewPageAsync();
        var secondPage = await secondContext.NewPageAsync();
        await firstPage.GotoAsync(firstSession.GetUrl("/demo/agentic_chat"));
        await secondPage.GotoAsync(secondSession.GetUrl("/demo/agentic_chat"));
        await Expect(firstPage.Locator(".dojo-demo")).ToHaveAttributeAsync("data-interactive", "true");
        await Expect(secondPage.Locator(".dojo-demo")).ToHaveAttributeAsync("data-interactive", "true");
        var firstScenario = firstPage.Locator("[data-dojo-scenario='agentic-chat']");
        var secondScenario = secondPage.Locator("[data-dojo-scenario='agentic-chat']");
        var firstCheckpoint = firstPage.Locator(".replay-checkpoint-status");
        var secondCheckpoint = secondPage.Locator(".replay-checkpoint-status");
        var firstAction = firstSession.Lock(script.GetLockName(0, 0));
        var firstStart = firstSession.Lock(script.GetLockName(1, 0));
        var firstFinal = firstSession.Lock(script.GetLockName(1, 1));
        var secondAction = secondSession.Lock(script.GetLockName(0, 0));
        var secondStart = secondSession.Lock(script.GetLockName(1, 0));
        var secondFinal = secondSession.Lock(script.GetLockName(1, 1));
        await using (firstAction)
        await using (firstStart)
        await using (firstFinal)
        await using (secondAction)
        await using (secondStart)
        await using (secondFinal)
        {
            await firstScenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Change background", Exact = true }).ClickAsync();
            await secondScenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Change background", Exact = true }).ClickAsync();

            const string expectedStyle = "background: linear-gradient(135deg, #ff9a9e, #fad0c4);";
            await Expect(firstScenario).ToHaveAttributeAsync("style", expectedStyle);
            await Expect(secondScenario).ToHaveAttributeAsync("style", expectedStyle);
            await Expect(firstCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "background-action");
            await Expect(secondCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "background-action");

            await firstAction.ReleaseAsync();

            var firstAssistant = firstScenario.Locator(
                ".sc-ai-turn .sc-ai-message--assistant .sc-ai-message__content");
            var secondAssistant = secondScenario.Locator(
                ".sc-ai-turn .sc-ai-message--assistant .sc-ai-message__content");
            await Expect(firstAssistant).ToHaveTextAsync("Background changed");
            await Expect(secondAssistant).ToHaveCountAsync(0);
            await Expect(firstCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-start");
            await Expect(secondCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "background-action");

            await secondAction.ReleaseAsync();
            await Expect(secondAssistant).ToHaveTextAsync("Background changed");
            await Expect(secondCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-start");

            await firstStart.ReleaseAsync();
            await secondStart.ReleaseAsync();
            await Expect(firstAssistant).ToHaveTextAsync("Background changed to a sunset gradient.");
            await Expect(secondAssistant).ToHaveTextAsync("Background changed to a sunset gradient.");
            await Expect(firstCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-final");
            await Expect(secondCheckpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "confirmation-final");

            await firstFinal.ReleaseAsync();
            await secondFinal.ReleaseAsync();
            await Expect(firstCheckpoint).ToHaveCountAsync(0);
            await Expect(secondCheckpoint).ToHaveCountAsync(0);
            await Expect(firstScenario.Locator(".sc-ai-input__send")).ToBeEnabledAsync();
            await Expect(secondScenario.Locator(".sc-ai-input__send")).ToBeEnabledAsync();
        }
    }
}
