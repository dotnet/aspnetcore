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
public partial class ToolBasedGenerativeUIScenarioTests : BrowserTest
{
    [TestMethod]
    public async Task GenerateHaiku_RendersAndNavigatesCarouselBeforeCompletingStream()
    {
        var script = ReplayCheckpointScript.Load("Dojo_ToolBasedGenerativeUI.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.ToolBasedGenerativeUI));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/tool_based_generative_ui"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='tool-based-generative-ui']");
        var carousel = scenario.Locator(".haiku-carousel");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");
        var reset = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Reset", Exact = true });
        var checkpoint = demo.Locator(".replay-checkpoint-status");

        await Expect(scenario.Locator(".dojo-scenario__header h2")).ToHaveTextAsync("Haiku Generator");
        await AssertPlaceholderHaikuAsync(carousel);
        await Expect(carousel.Locator(".haiku-carousel__nav")).ToHaveCountAsync(0);

        var action = session.Lock(script.GetLockName(0, 0));
        var summaryStart = session.Lock(script.GetLockName(1, 0));
        var summaryFinal = session.Lock(script.GetLockName(1, 1));
        await using (action)
        await using (summaryStart)
        await using (summaryFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Nature Haiku", Exact = true }).ClickAsync();

            var turn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(1);
            await Expect(turn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Write me a haiku about nature");
            await AssertGeneratedHaikuAsync(carousel);
            await AssertGeneratedCarouselControlsAsync(carousel);
            await Expect(send).ToBeDisabledAsync();
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-action");
            await Expect(reset).ToBeDisabledAsync();
            await reset.EvaluateAsync("button => button.click()");
            await AssertGeneratedHaikuAsync(carousel);
            await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(1);
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-action");

            await action.ReleaseAsync();

            var assistant = turn.Locator(".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(assistant).ToHaveTextAsync("Your nature haiku is ready");
            await Expect(assistant).ToHaveClassAsync(
                "sc-ai-message__content sc-ai-message__content--streaming");
            await AssertGeneratedHaikuAsync(carousel);
            await AssertGeneratedCarouselControlsAsync(carousel);

            var carouselButtons = carousel.Locator(".haiku-carousel__btn");
            await carouselButtons.Nth(0).ClickAsync();
            await AssertPlaceholderHaikuAsync(carousel);
            await Expect(carousel.Locator(".haiku-carousel__counter")).ToHaveTextAsync("1 / 2");
            await Expect(carouselButtons.Nth(0)).ToBeDisabledAsync();
            await Expect(carouselButtons.Nth(1)).ToBeEnabledAsync();

            await carouselButtons.Nth(1).ClickAsync();
            await AssertGeneratedHaikuAsync(carousel);
            await AssertGeneratedCarouselControlsAsync(carousel);
            await Expect(send).ToBeDisabledAsync();
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-summary-start");

            await summaryStart.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync(
                "Your nature haiku is ready\u2014a quiet pond awakened by a frog.");
            await Expect(assistant).ToHaveClassAsync(
                "sc-ai-message__content sc-ai-message__content--streaming");
            await AssertGeneratedHaikuAsync(carousel);
            await AssertGeneratedCarouselControlsAsync(carousel);
            await Expect(send).ToBeDisabledAsync();
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-summary-final");

            await summaryFinal.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync(
                "Your nature haiku is ready\u2014a quiet pond awakened by a frog.");
            await Expect(assistant).ToHaveClassAsync("sc-ai-message__content");
            await AssertGeneratedHaikuAsync(carousel);
            await AssertGeneratedCarouselControlsAsync(carousel);
            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(reset).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }

        await reset.ClickAsync();
        await AssertPlaceholderHaikuAsync(carousel);
        await Expect(carousel.Locator(".haiku-carousel__nav")).ToHaveCountAsync(0);
        await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(0);

        session.ResetReplay();
        var resetAction = session.Lock(script.GetLockName(0, 0));
        var resetSummaryStart = session.Lock(script.GetLockName(1, 0));
        var resetSummaryFinal = session.Lock(script.GetLockName(1, 1));
        await using (resetAction)
        await using (resetSummaryStart)
        await using (resetSummaryFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Nature Haiku", Exact = true }).ClickAsync();

            var resetTurn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(resetTurn.Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Write me a haiku about nature");
            await AssertGeneratedHaikuAsync(carousel);
            await AssertGeneratedCarouselControlsAsync(carousel);
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-action");

            await resetAction.ReleaseAsync();

            var resetAssistant = resetTurn.Locator(
                ".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(resetAssistant).ToHaveTextAsync("Your nature haiku is ready");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-summary-start");

            await resetSummaryStart.ReleaseAsync();

            await Expect(resetAssistant).ToHaveTextAsync(
                "Your nature haiku is ready\u2014a quiet pond awakened by a frog.");
            await Expect(checkpoint).ToHaveAttributeAsync(
                "data-replay-checkpoint",
                "haiku-summary-final");

            await resetSummaryFinal.ReleaseAsync();

            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
        }
    }

    private static async Task AssertPlaceholderHaikuAsync(ILocator carousel)
    {
        var card = carousel.Locator(".haiku-card");
        await Expect(card).ToHaveCountAsync(1);
        await Expect(card).ToHaveClassAsync("haiku-card");
        await Expect(card).ToHaveAttributeAsync(
            "style",
            "background: linear-gradient(135deg, #667eea, #764ba2);");
        await Expect(card.Locator(".haiku-card__japanese p")).ToHaveCountAsync(3);
        await Expect(card.Locator(".haiku-card__japanese p").Nth(0)).ToHaveTextAsync("ここに一句");
        await Expect(card.Locator(".haiku-card__japanese p").Nth(1)).ToHaveTextAsync("仮のうた置く");
        await Expect(card.Locator(".haiku-card__japanese p").Nth(2)).ToHaveTextAsync("春を待つ");
        await Expect(card.Locator(".haiku-card__english p")).ToHaveCountAsync(3);
        await Expect(card.Locator(".haiku-card__english p").Nth(0))
            .ToHaveTextAsync("A placeholder verse\u2014");
        await Expect(card.Locator(".haiku-card__english p").Nth(1))
            .ToHaveTextAsync("Resting here for now,");
        await Expect(card.Locator(".haiku-card__english p").Nth(2))
            .ToHaveTextAsync("Awaiting your words.");
    }

    private static async Task AssertGeneratedHaikuAsync(ILocator carousel)
    {
        var card = carousel.Locator(".haiku-card");
        await Expect(card).ToHaveCountAsync(1);
        await Expect(card).ToHaveClassAsync("haiku-card");
        await Expect(card).ToHaveAttributeAsync(
            "style",
            "background: linear-gradient(135deg, #134e5e, #71b280);");
        await Expect(card.Locator(".haiku-card__japanese p")).ToHaveCountAsync(3);
        await Expect(card.Locator(".haiku-card__japanese p").Nth(0)).ToHaveTextAsync("古池や");
        await Expect(card.Locator(".haiku-card__japanese p").Nth(1)).ToHaveTextAsync("蛙飛びこむ");
        await Expect(card.Locator(".haiku-card__japanese p").Nth(2)).ToHaveTextAsync("水の音");
        await Expect(card.Locator(".haiku-card__english p")).ToHaveCountAsync(3);
        await Expect(card.Locator(".haiku-card__english p").Nth(0))
            .ToHaveTextAsync("An ancient pond\u2014");
        await Expect(card.Locator(".haiku-card__english p").Nth(1))
            .ToHaveTextAsync("A frog leaps in,");
        await Expect(card.Locator(".haiku-card__english p").Nth(2))
            .ToHaveTextAsync("The sound of water.");
    }

    private static async Task AssertGeneratedCarouselControlsAsync(ILocator carousel)
    {
        await Expect(carousel).ToHaveClassAsync("haiku-carousel");
        await Expect(carousel.Locator(".haiku-carousel__nav")).ToHaveCountAsync(1);
        var buttons = carousel.Locator(".haiku-carousel__btn");
        await Expect(buttons).ToHaveCountAsync(2);
        await Expect(buttons.Nth(0)).ToHaveClassAsync("haiku-carousel__btn");
        await Expect(buttons.Nth(0)).ToBeEnabledAsync();
        await Expect(buttons.Nth(1)).ToHaveClassAsync("haiku-carousel__btn");
        await Expect(buttons.Nth(1)).ToBeDisabledAsync();
        await Expect(carousel.Locator(".haiku-carousel__counter")).ToHaveClassAsync(
            "haiku-carousel__counter");
        await Expect(carousel.Locator(".haiku-carousel__counter")).ToHaveTextAsync("2 / 2");
    }
}
