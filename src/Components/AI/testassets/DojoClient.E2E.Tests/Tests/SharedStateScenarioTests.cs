// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUIDojoApi;
using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

// The browser edits typed state in DojoClient, which serializes it through AGUIChatClient.
// Only the API's model is recorded; thread identity, state, HTTP, and SSE remain real.
[UITest]
public partial class SharedStateScenarioTests : BrowserTest
{
    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;
    private string _firstPrompt = null!;
    private string _secondPrompt = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        var runId = Guid.NewGuid().ToString("N");
        _firstPrompt = $"Create a delicious Italian pasta recipe. ({runId})";
        _secondPrompt = $"Improve the recipe with fresh herbs. ({runId})";
        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.SharedState));
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });
        _checkpoints = new ApiCheckpointClient(_api);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
        await _page.GotoAsync($"{_ui.TestUrl}/shared_state");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    public async Task RecipeEditor_ForwardsLocalEditsAndPreservesThemAcrossAgentUpdates()
    {
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
        await Expect(editor.GetByLabel("Recipe title"))
            .ToHaveValueAsync("Sunday Garden Pasta");

        await _checkpoints.ReleaseAsync(_firstPrompt, "before-italian-recipe");

        await Expect(editor.GetByLabel("Recipe title"))
            .ToHaveValueAsync("Italian Garden Pasta");
        await Expect(editor.GetByLabel("Cooking time")).ToHaveValueAsync("30 min");
        await Expect(editor.GetByLabel("Ingredient name")).ToHaveCountAsync(4);
        await Expect(editor.GetByLabel("Ingredient name").Nth(1)).ToHaveValueAsync("Zucchini");
        await Expect(editor.GetByLabel("Ingredient amount").Nth(1)).ToHaveValueAsync("2, sliced");
        await Expect(editor.GetByLabel("High Protein", new() { Exact = true })).ToBeCheckedAsync();

        await _checkpoints.ReleaseAsync(_firstPrompt, "before-italian-summary");
        await Expect(scenario.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content").Last)
            .ToHaveTextAsync("I created an Italian garden pasta and kept your zucchini.");
        await Expect(send).ToBeEnabledAsync();

        await editor.GetByLabel("Ingredient amount").Nth(1).FillAsync("3, sliced");
        await input.FillAsync(_secondPrompt);
        await send.ClickAsync();
        await Expect(send).ToBeDisabledAsync();

        await _checkpoints.ReleaseAsync(_secondPrompt, "before-herbed-recipe");

        await Expect(editor.GetByLabel("Recipe title"))
            .ToHaveValueAsync("Herbed Italian Garden Pasta");
        await Expect(editor.GetByLabel("Ingredient name")).ToHaveCountAsync(5);
        await Expect(editor.GetByLabel("Ingredient name").Nth(1)).ToHaveValueAsync("Zucchini");
        await Expect(editor.GetByLabel("Ingredient amount").Nth(1)).ToHaveValueAsync("3, sliced");
        await Expect(editor.GetByLabel("Ingredient name").Last).ToHaveValueAsync("Fresh Basil");
        await Expect(editor.GetByLabel("High Protein", new() { Exact = true })).ToBeCheckedAsync();

        await _checkpoints.ReleaseAsync(_secondPrompt, "before-herbed-summary");
        await Expect(scenario.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content").Last)
            .ToHaveTextAsync("I added fresh basil while preserving your latest recipe edits.");
        await Expect(send).ToBeEnabledAsync();
    }
}
