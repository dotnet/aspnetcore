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
    private const string InitialDocument =
        "# Harbor Notes\n\nThe crew is preparing for a quiet voyage.";
    private const string PirateOpening =
        "# Candy Beard's Voyage\n\nCandy Beard sailed from Gumdrop Harbor in search of the Sugar Star.";
    private const string PirateDocument =
        "# Candy Beard's Voyage\n\nCandy Beard sailed from Gumdrop Harbor in search of the Sugar Star." +
        "\n\nWhen dark clouds gathered, the crew shared their courage and found the way home.";
    private const string EditedPirateDocument =
        PirateDocument + "\n\nThe map now points toward Mermaid Lagoon.";
    private const string CourageDocument =
        EditedPirateDocument +
        "\n\nCourage joined the crew and offered to guide them through Mermaid Lagoon.";

    [TestMethod]
    public async Task DocumentEditor_StreamsDiffAndSupportsAcceptAndReject()
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
        var editor = panel.GetByRole(
            AriaRole.Textbox,
            new() { Name = "Document editor", Exact = true });
        var checkpoint = demo.Locator(".replay-checkpoint-status");
        var send = scenario.Locator(".sc-ai-input__send");
        var reset = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Reset", Exact = true });

        await Expect(editor).ToHaveClassAsync("document-editor__input");
        await Expect(editor).ToHaveAttributeAsync(
            "placeholder",
            "Write whatever you want here in Markdown format...");
        await editor.FillAsync(InitialDocument);
        await Expect(editor).ToHaveValueAsync(InitialDocument);
        await AssertNoInternalMetadataAsync(editor);
        await AssertSuggestionsAsync(scenario);

        var pirateTitle = session.Lock(script.GetLockName(0, 0));
        var pirateOpening = session.Lock(script.GetLockName(0, 1));
        var pirateCandidateComplete = session.Lock(script.GetLockName(0, 2));
        var pirateConfirmation = session.Lock(script.GetLockName(0, 3));
        var pirateSummary = session.Lock(script.GetLockName(1, 0));
        await using (pirateTitle)
        await using (pirateOpening)
        await using (pirateCandidateComplete)
        await using (pirateConfirmation)
        await using (pirateSummary)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Write a pirate story", Exact = true }).ClickAsync();

            await AssertReadOnlyDiffAsync(editor, contains: "Candy Beard's Voyage");
            await Expect(editor).Not.ToContainTextAsync("Sugar Star");
            await Expect(editor).Not.ToContainTextAsync("shared their courage");
            await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(0);
            await Expect(send).ToBeDisabledAsync();
            await Expect(reset).ToBeDisabledAsync();
            await reset.EvaluateAsync("button => button.click()");
            await AssertCheckpointAsync(checkpoint, "pirate-title");

            await pirateTitle.ReleaseAsync();

            await AssertReadOnlyDiffAsync(editor, contains: PirateOpening);
            await Expect(editor).Not.ToContainTextAsync("shared their courage");
            await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(0);
            await AssertCheckpointAsync(checkpoint, "pirate-opening");

            await pirateOpening.ReleaseAsync();

            await AssertReadOnlyDiffAsync(editor, contains: PirateDocument);
            await Expect(scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true })).ToHaveCountAsync(0);
            await AssertCheckpointAsync(checkpoint, "pirate-candidate-complete");

            await pirateCandidateComplete.ReleaseAsync();

            var dialog = scenario.Locator(".confirm-changes").Last;
            await Expect(dialog.Locator(".confirm-changes__message"))
                .ToHaveTextAsync("Do you want to accept the changes?");
            await AssertCheckpointAsync(checkpoint, "pirate-confirmation");

            await pirateConfirmation.ReleaseAsync();
            await dialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true }).ClickAsync();

            await Expect(editor).ToHaveClassAsync("document-editor__surface");
            await Expect(editor).ToContainTextAsync(PirateDocument);
            await Expect(editor.Locator("em")).ToHaveCountAsync(0);
            await Expect(editor.Locator("s")).ToHaveCountAsync(0);
            await AssertNoInternalMetadataAsync(editor);
            await Expect(dialog.Locator(".confirm-changes__status"))
                .ToHaveTextAsync("\u2713 Accepted");
            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--assistant .sc-ai-message__content"))
                .ToHaveTextAsync("Candy Beard's voyage is ready.");
            await AssertCheckpointAsync(checkpoint, "pirate-summary-final");

            await pirateSummary.ReleaseAsync();

            await Expect(editor).ToHaveClassAsync("document-editor__input");
            await Expect(editor).ToHaveValueAsync(PirateDocument);
            await AssertNoInternalMetadataAsync(editor);
            await Expect(send).ToBeEnabledAsync();
            await Expect(reset).ToBeEnabledAsync();
            await Expect(checkpoint).ToHaveCountAsync(0);
        }

        await editor.FillAsync(EditedPirateDocument);
        await Expect(editor).ToHaveValueAsync(EditedPirateDocument);

        var courageDraft = session.Lock(script.GetLockName(2, 0));
        var courageCandidateComplete = session.Lock(script.GetLockName(2, 1));
        var courageConfirmation = session.Lock(script.GetLockName(2, 2));
        var courageSummary = session.Lock(script.GetLockName(3, 0));
        await using (courageDraft)
        await using (courageCandidateComplete)
        await using (courageConfirmation)
        await using (courageSummary)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Add character", Exact = true }).ClickAsync();

            await AssertReadOnlyDiffAsync(
                editor,
                contains: "Courage joined the crew",
                expectRemoval: false);
            await Expect(editor).Not.ToContainTextAsync("offered to guide");
            await AssertCheckpointAsync(checkpoint, "courage-draft");

            await courageDraft.ReleaseAsync();

            await AssertReadOnlyDiffAsync(editor, contains: CourageDocument, expectRemoval: false);
            await Expect(scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Reject", Exact = true })).ToHaveCountAsync(0);
            await AssertCheckpointAsync(checkpoint, "courage-candidate-complete");

            await courageCandidateComplete.ReleaseAsync();

            var dialog = scenario.Locator(".confirm-changes").Last;
            await AssertCheckpointAsync(checkpoint, "courage-confirmation");

            await courageConfirmation.ReleaseAsync();
            await dialog.GetByRole(
                AriaRole.Button,
                new() { Name = "Reject", Exact = true }).ClickAsync();

            await Expect(editor).ToHaveClassAsync("document-editor__surface");
            await Expect(editor).ToContainTextAsync(EditedPirateDocument);
            await Expect(editor).Not.ToContainTextAsync("Courage joined the crew");
            await AssertNoInternalMetadataAsync(editor);
            await Expect(dialog.Locator(".confirm-changes__status"))
                .ToHaveTextAsync("\u2717 Rejected");
            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--assistant .sc-ai-message__content"))
                .ToHaveTextAsync("I left the document unchanged.");
            await AssertCheckpointAsync(checkpoint, "courage-summary-final");

            await courageSummary.ReleaseAsync();

            await Expect(editor).ToHaveClassAsync("document-editor__input");
            await Expect(editor).ToHaveValueAsync(EditedPirateDocument);
            await AssertNoInternalMetadataAsync(editor);
            await Expect(send).ToBeEnabledAsync();
            await Expect(reset).ToBeEnabledAsync();
            await Expect(checkpoint).ToHaveCountAsync(0);
        }

        await reset.ClickAsync();
        await Expect(editor).ToHaveClassAsync("document-editor__input");
        await Expect(editor).ToHaveValueAsync("");
        await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(0);

        await editor.FillAsync(InitialDocument);
        session.ResetReplay();
        var resetFrames = Enumerable.Range(0, 4)
            .Select(index => session.Lock(script.GetLockName(0, index)))
            .ToArray();
        var resetSummary = session.Lock(script.GetLockName(1, 0));
        await using (resetFrames[0])
        await using (resetFrames[1])
        await using (resetFrames[2])
        await using (resetFrames[3])
        await using (resetSummary)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Write a pirate story", Exact = true }).ClickAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-title");
            await resetFrames[0].ReleaseAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-opening");
            await resetFrames[1].ReleaseAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-candidate-complete");
            await resetFrames[2].ReleaseAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-confirmation");
            await resetFrames[3].ReleaseAsync();
            await scenario.Locator(".confirm-changes").Last.GetByRole(
                AriaRole.Button,
                new() { Name = "Confirm", Exact = true }).ClickAsync();
            await AssertCheckpointAsync(checkpoint, "pirate-summary-final");
            await resetSummary.ReleaseAsync();
            await Expect(editor).ToHaveClassAsync("document-editor__input");
            await Expect(editor).ToHaveValueAsync(PirateDocument);
            await Expect(send).ToBeEnabledAsync();
        }
    }

    private static async Task AssertSuggestionsAsync(ILocator scenario)
    {
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Write a pirate story", Exact = true })).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Write a mermaid story", Exact = true })).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Add character", Exact = true })).ToBeEnabledAsync();
    }

    private static async Task AssertReadOnlyDiffAsync(
        ILocator editor,
        string contains,
        bool expectRemoval = true)
    {
        await Expect(editor).ToHaveClassAsync("document-editor__surface");
        await Expect(editor).ToHaveAttributeAsync("aria-readonly", "true");
        var proposedDocument = await editor.Locator(".document-editor__diff").EvaluateAsync<string>(
            """
            element => {
                const clone = element.cloneNode(true);
                clone.querySelectorAll('s').forEach(item => item.remove());
                return clone.textContent;
            }
            """);
        StringAssert.Contains(proposedDocument, contains);
        Assert.IsGreaterThan(0, await editor.Locator("em").CountAsync());
        if (expectRemoval)
        {
            Assert.IsGreaterThan(0, await editor.Locator("s").CountAsync());
        }
        else
        {
            await Expect(editor.Locator("s")).ToHaveCountAsync(0);
        }
        await AssertNoInternalMetadataAsync(editor);
    }

    private static async Task AssertNoInternalMetadataAsync(ILocator editor)
    {
        var forbiddenValues = new[]
        {
            "runStartDocument",
            "_runStartDocument",
            "write_document_local",
            "confirm_changes",
        };
        var html = await editor.InnerHTMLAsync();
        var text = await editor.TextContentAsync() ?? "";
        foreach (var value in forbiddenValues)
        {
            Assert.DoesNotContain(value, text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(value, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task AssertCheckpointAsync(ILocator checkpoint, string name)
    {
        await Expect(checkpoint).ToHaveAttributeAsync("data-replay-checkpoint", name);
    }
}
