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
public partial class PredictiveStateUpdatesScenarioTests : BrowserTest
{
    private const string PirateTitle = "The Adventures of Captain Ember";
    private const string PirateOpening =
        "Captain Ember sailed the moonlit sea in search of the legendary Star Compass.";
    private const string PirateEnding =
        "When a storm scattered her crew, she followed a brave parrot's song through the fog " +
        "and brought everyone safely home.";

    [TestMethod]
    public async Task DocumentState_StreamsIntoPreviewAndWaitsForConfirmation()
    {
        var script = ReplayCheckpointScript.Load("Dojo_PredictiveStateUpdates.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.PredictiveStateUpdates));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/predictive_state_updates"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='predictive-state-updates']");
        var panel = scenario.Locator(".document-panel");
        var preview = panel.Locator(".document-preview");
        var content = preview.Locator(".document-preview__content");
        var checkpoint = demo.Locator(".replay-checkpoint-status");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");

        await Expect(scenario.Locator(".predictive-chat .dojo-scenario__header h2"))
            .ToHaveTextAsync("AI Document Editor");
        await Expect(scenario.Locator(".dojo-reset-button")).ToHaveTextAsync("\u00d7");
        await Expect(preview).ToHaveClassAsync("document-preview document-preview--empty");
        await Expect(preview.Locator(".document-preview__placeholder"))
            .ToHaveTextAsync("Write whatever you want here in Markdown format...");
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Write a pirate story", Exact = true })).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Write a mermaid story", Exact = true })).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Add character", Exact = true })).ToBeEnabledAsync();
        await Expect(input).ToHaveAttributeAsync("placeholder", "Type a message...");
        await Expect(send).ToBeEnabledAsync();
        await Expect(checkpoint).ToHaveCountAsync(0);

        var titleFrame = session.Lock(script.GetLockName(0, 0));
        var contentFrame = session.Lock(script.GetLockName(0, 1));
        var confirmationFrame = session.Lock(script.GetLockName(0, 2));
        var summaryFinal = session.Lock(script.GetLockName(1, 0));
        await using (titleFrame)
        await using (contentFrame)
        await using (confirmationFrame)
        await using (summaryFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Write a pirate story", Exact = true }).ClickAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [],
                expectedWordCount: 5,
                isStreaming: true);
            await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(0);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-title");

            await titleFrame.ReleaseAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [PirateOpening],
                expectedWordCount: 18,
                isStreaming: true);
            await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(0);
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-content");

            await contentFrame.ReleaseAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [PirateOpening, PirateEnding],
                expectedWordCount: 38,
                isStreaming: true);
            var dialog = scenario.Locator(".confirm-changes");
            await Expect(dialog).ToHaveClassAsync("confirm-changes");
            await Expect(dialog.Locator(".confirm-changes__header h3"))
                .ToHaveTextAsync("Confirm Changes");
            await Expect(dialog.Locator(".confirm-changes__message"))
                .ToHaveTextAsync("Do you want to accept the changes?");
            var reject = dialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Reject", Exact = true });
            await Expect(reject).ToHaveClassAsync(
                "confirm-changes__btn confirm-changes__btn--reject");
            await Expect(reject).ToBeEnabledAsync();
            var confirm = dialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true });
            await Expect(confirm).ToHaveClassAsync(
                "confirm-changes__btn confirm-changes__btn--accept");
            await Expect(confirm).ToBeEnabledAsync();
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-confirmation");

            await confirmationFrame.ReleaseAsync();
            await confirm.ClickAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [PirateOpening, PirateEnding],
                expectedWordCount: 38,
                isStreaming: false);
            await Expect(dialog).ToHaveClassAsync(
                "confirm-changes confirm-changes--responded");
            await Expect(dialog.Locator(".confirm-changes__status"))
                .ToHaveClassAsync(
                    "confirm-changes__status confirm-changes__status--accepted");
            await Expect(dialog.Locator(".confirm-changes__status"))
                .ToHaveTextAsync("\u2713 Accepted");
            await Expect(dialog.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
            var assistant = scenario
                .Locator(".sc-ai-turn")
                .Last
                .Locator(".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(assistant).ToHaveTextAsync(
                "I wrote a short pirate adventure about Captain Ember finding her crew with " +
                "courage and a parrot's song.");
            await Expect(assistant).ToHaveClassAsync(
                "sc-ai-message__content sc-ai-message__content--streaming");
            await Expect(send).ToBeDisabledAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-summary-final");

            await summaryFinal.ReleaseAsync();

            await Expect(assistant).ToHaveTextAsync(
                "I wrote a short pirate adventure about Captain Ember finding her crew with " +
                "courage and a parrot's song.");
            await Expect(assistant).ToHaveClassAsync("sc-ai-message__content");
            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }

        await scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Reset", Exact = true }).ClickAsync();
        await Expect(preview).ToHaveClassAsync("document-preview document-preview--empty");
        await Expect(preview.Locator(".document-preview__placeholder"))
            .ToHaveTextAsync("Write whatever you want here in Markdown format...");
        await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(0);

        session.ResetReplay();
        var resetTitleFrame = session.Lock(script.GetLockName(0, 0));
        var resetContentFrame = session.Lock(script.GetLockName(0, 1));
        var resetConfirmationFrame = session.Lock(script.GetLockName(0, 2));
        var resetSummaryFinal = session.Lock(script.GetLockName(1, 0));
        await using (resetTitleFrame)
        await using (resetContentFrame)
        await using (resetConfirmationFrame)
        await using (resetSummaryFinal)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Write a pirate story", Exact = true }).ClickAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [],
                expectedWordCount: 5,
                isStreaming: true);
            await AssertCheckpointAsync(checkpoint, "pirate-title");

            await resetTitleFrame.ReleaseAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [PirateOpening],
                expectedWordCount: 18,
                isStreaming: true);
            await AssertCheckpointAsync(checkpoint, "pirate-content");

            await resetContentFrame.ReleaseAsync();

            await AssertDocumentAsync(
                panel,
                preview,
                content,
                PirateTitle,
                [PirateOpening, PirateEnding],
                expectedWordCount: 38,
                isStreaming: true);
            var resetDialog = scenario.Locator(".confirm-changes");
            await Expect(resetDialog.Locator(".confirm-changes__message"))
                .ToHaveTextAsync("Do you want to accept the changes?");
            await AssertCheckpointAsync(checkpoint, "pirate-confirmation");

            await resetConfirmationFrame.ReleaseAsync();
            await resetDialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true }).ClickAsync();

            var resetAssistant = scenario
                .Locator(".sc-ai-turn")
                .Last
                .Locator(".sc-ai-message--assistant .sc-ai-message__content");
            await Expect(resetAssistant).ToHaveTextAsync(
                "I wrote a short pirate adventure about Captain Ember finding her crew with " +
                "courage and a parrot's song.");
            await AssertCheckpointAsync(checkpoint, "pirate-summary-final");

            await resetSummaryFinal.ReleaseAsync();

            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
        }
    }

    private static async Task AssertDocumentAsync(
        ILocator panel,
        ILocator preview,
        ILocator content,
        string title,
        string[] paragraphs,
        int expectedWordCount,
        bool isStreaming)
    {
        await Expect(panel).ToHaveClassAsync(
            isStreaming
                ? "document-panel document-panel--streaming"
                : "document-panel ");
        await Expect(preview).ToHaveClassAsync(
            isStreaming
                ? "document-preview document-preview--streaming"
                : "document-preview ");
        await Expect(content.Locator("h1")).ToHaveTextAsync(title);
        var paragraphLocators = content.Locator("p");
        await Expect(paragraphLocators).ToHaveCountAsync(paragraphs.Length);
        for (var index = 0; index < paragraphs.Length; index++)
        {
            await Expect(paragraphLocators.Nth(index)).ToHaveTextAsync(paragraphs[index]);
        }

        var wordCount = await content.EvaluateAsync<int>(
            "element => element.innerText.match(/\\S+/g)?.length ?? 0");
        Assert.AreEqual(expectedWordCount, wordCount);
        await Expect(preview.Locator(".document-preview__cursor"))
            .ToHaveCountAsync(isStreaming ? 1 : 0);
    }

    private static async Task AssertCheckpointAsync(ILocator checkpoint, string name)
    {
        await Expect(checkpoint).ToHaveAttributeAsync("data-replay-checkpoint", name);
    }
}
