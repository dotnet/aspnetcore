// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[UITest]
public partial class SharedStateScenarioTests : DojoTestBase
{
    private DojoTestSession _dojo = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;
    private string _firstPrompt = null!;

    private async Task InitializeScenarioAsync(string backend)
    {
        var runId = Guid.NewGuid().ToString("N");
        _firstPrompt = $"Create a delicious Italian pasta recipe. ({runId})";
        _dojo = await GetDojoAsync(backend, DojoRecording.SharedState);
        _checkpoints = _dojo.Checkpoints;

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_dojo.UI));
        _page = await context.NewPageAsync();
        await _page.GotoAsync(_dojo.GetScenarioUrl("/shared_state"));
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task RecipeEditor_ReplacesLocalStateWithItalianRecipeSnapshot(string backend)
    {
        await InitializeScenarioAsync(backend);
        var scenario = _page.Locator("[data-scenario='shared_state']");
        var editor = scenario.Locator(".recipe-editor");
        var input = scenario.Locator("textarea.sc-ai-input__textarea");
        var send = scenario.Locator("button.sc-ai-input__send");

        await Expect(editor.GetByLabel("Recipe title"))
            .ToHaveValueAsync("Make Your Recipe");
        await editor.GetByLabel("Recipe title").FillAsync("Sunday Garden Pasta");
        await editor.GetByLabel("Skill level").SelectOptionAsync("Beginner");
        await editor.GetByLabel("High Protein", new() { Exact = true }).CheckAsync();
        await editor.GetByLabel("Ingredient name").First.FillAsync("Zucchini");
        await editor.GetByLabel("Ingredient amount").First.FillAsync("2, sliced");

        await input.FillAsync(_firstPrompt);
        await send.ClickAsync();
        await Expect(send).ToBeDisabledAsync();
        var improve = editor.Locator(".recipe-editor__improve-btn");
        await Expect(improve).ToBeDisabledAsync();
        await Expect(improve).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(improve.GetByRole(AriaRole.Status)).ToHaveTextAsync("Please Wait...");
        await Expect(scenario.GetByRole(AriaRole.Button, new() { Name = "Stop response" }))
            .ToBeVisibleAsync();
        await Expect(editor.GetByLabel("Recipe title"))
            .ToHaveValueAsync("Sunday Garden Pasta");

        await _checkpoints.ReleaseAsync(_firstPrompt, "before-italian-recipe");

        await Expect(editor.GetByLabel("Recipe title"))
            .ToHaveValueAsync("Classic Italian Carbonara");
        await Expect(editor.GetByLabel("Skill level")).ToHaveValueAsync("Intermediate");
        await Expect(editor.GetByLabel("Cooking time")).ToHaveValueAsync("45 min");
        await Expect(editor.GetByLabel("Ingredient name")).ToHaveCountAsync(6);
        await Expect(editor.GetByLabel("Ingredient name").Nth(0)).ToHaveValueAsync("Spaghetti");
        await Expect(editor.GetByLabel("Ingredient amount").Nth(0)).ToHaveValueAsync("400g");
        await Expect(editor.GetByLabel("Ingredient name").Nth(1))
            .ToHaveValueAsync("Guanciale (Pork Jowl)");
        await Expect(editor.GetByLabel("Ingredient name").Nth(5)).ToHaveValueAsync("Black Pepper");
        await Expect(editor.GetByLabel("High Protein", new() { Exact = true }))
            .Not.ToBeCheckedAsync();

        await _checkpoints.ReleaseAsync(_firstPrompt, "before-italian-summary");
        await Expect(scenario.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content").Last)
            .ToContainTextAsync("Classic Italian Carbonara");
        await Expect(send).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(AriaRole.Button, new() { Name = "Stop response" }))
            .ToBeHiddenAsync();
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task RecipeEditor_MatchesDojoChatAndResponsiveControls(string backend)
    {
        await InitializeScenarioAsync(backend);
        var scenario = _page.Locator("[data-scenario='shared_state']");
        var editor = scenario.Locator(".recipe-editor");
        var chat = scenario.GetByRole(
            AriaRole.Complementary,
            new() { Name = "Copilot chat sidebar" });

        await Expect(editor.GetByLabel("Cooking time").Locator("option"))
            .ToHaveTextAsync(["5 min", "15 min", "30 min", "45 min", "60+ min"]);
        await Expect(editor.GetByLabel("Budget-Friendly")).ToBeVisibleAsync();
        await Expect(editor.GetByLabel("One-Pot Meal")).ToBeVisibleAsync();
        await Expect(editor.GetByLabel("Gluten-Free")).ToHaveCountAsync(0);

        await scenario.GetByRole(AriaRole.Button, new() { Name = "Close chat" }).ClickAsync();
        await Expect(chat).ToBeHiddenAsync();
        await Expect(editor.GetByLabel("Recipe title")).ToHaveValueAsync("Make Your Recipe");

        await scenario.GetByRole(AriaRole.Button, new() { Name = "Open chat" }).ClickAsync();
        await Expect(chat).ToBeVisibleAsync();

        await _page.SetViewportSizeAsync(390, 844);
        var mobileToggle = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "AI Recipe Assistant Ask me to craft recipes" });
        var chatContent = scenario.Locator("#shared-state-chat-content");

        await Expect(mobileToggle).ToBeVisibleAsync();
        await Expect(mobileToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(chatContent).ToBeHiddenAsync();

        await mobileToggle.ClickAsync();
        await Expect(mobileToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(chatContent).ToBeVisibleAsync();
    }
}
