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
public partial class SharedStateScenarioTests : BrowserTest
{
    [TestMethod]
    public async Task RecipeEditor_SynchronizesStateAndRunsImproveWithAI()
    {
        var script = ReplayCheckpointScript.Load("Dojo_SharedState.recording.json");
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(nameof(ChatClientOverrides.SharedState));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var session = await ReplayTestSession.CreateAsync(server, context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetUrl("/demo/shared_state"));

        var demo = page.Locator(".dojo-demo");
        await Expect(demo).ToHaveAttributeAsync("data-interactive", "true");
        var scenario = demo.Locator("[data-dojo-scenario='shared-state']");
        var editor = scenario.Locator(".recipe-editor");
        var checkpoint = demo.Locator(".replay-checkpoint-status");
        var input = scenario.Locator(".sc-ai-input__textarea");
        var send = scenario.Locator(".sc-ai-input__send");
        var reset = scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Reset", Exact = true });
        var improve = editor.Locator(".recipe-editor__improve-btn");

        await AssertRecipeAsync(
            editor,
            "Make Your Recipe",
            "Intermediate",
            "45 min",
            [],
            [
                ("\U0001F955", "Carrots", "3 large, grated"),
                ("\U0001F33E", "All-Purpose Flour", "2 cups"),
            ],
            ["Preheat oven to 350\u00b0F (175\u00b0C)"]);
        await AssertSuggestionsAsync(scenario);

        await CustomizeRecipeAsync(editor);
        await AssertCustomizedRecipeAsync(editor);
        var stableEditorHeight = await GetEditorHeightAsync(editor);

        var italianTitle = session.Lock(script.GetLockName(0, 0));
        var italianDetails = session.Lock(script.GetLockName(0, 1));
        var italianIngredients = session.Lock(script.GetLockName(0, 2));
        var italianComplete = session.Lock(script.GetLockName(0, 3));
        await using (italianTitle)
        await using (italianDetails)
        await using (italianIngredients)
        await using (italianComplete)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Create Italian recipe", Exact = true }).ClickAsync();

            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Create a delicious Italian pasta recipe.");
            await AssertChangedSectionAsync(editor, "title");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await Expect(editor.Locator("input[type='text']").First)
                .ToHaveValueAsync("Italian Garden Pasta");
            await Expect(send).ToBeDisabledAsync();
            await Expect(improve).ToBeDisabledAsync();
            await Expect(improve).ToHaveTextAsync("Please Wait...");
            await Expect(reset).ToBeDisabledAsync();
            await reset.EvaluateAsync("button => button.click()");
            await AssertCheckpointAsync(checkpoint, "italian-title");

            await italianTitle.ReleaseAsync();

            await AssertChangedSectionAsync(editor, "details");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await Expect(editor.Locator(".recipe-editor__row select").Nth(0))
                .ToHaveValueAsync("Intermediate");
            await AssertCheckpointAsync(checkpoint, "italian-details");

            await italianDetails.ReleaseAsync();

            await AssertChangedSectionAsync(editor, "ingredients");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            var ingredientRows = editor.Locator(".ingredient-row");
            await Expect(ingredientRows).ToHaveCountAsync(4);
            await Expect(ingredientRows.Nth(0).Locator(".ingredient-row__name"))
                .ToHaveValueAsync("Spaghetti");
            await AssertCheckpointAsync(checkpoint, "italian-ingredients");

            await italianIngredients.ReleaseAsync();

            await AssertChangedSectionAsync(editor, "instructions");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await AssertItalianRecipeAsync(editor);
            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--assistant .sc-ai-message__content"))
                .ToHaveTextAsync(
                    "I've turned your recipe into an Italian garden pasta with spaghetti, " +
                    "zucchini, tomatoes, and a protein-rich ricotta sauce.");
            await AssertCheckpointAsync(checkpoint, "italian-complete");

            await italianComplete.ReleaseAsync();

            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(improve).ToBeEnabledAsync();
            await Expect(improve).ToHaveTextAsync("Improve with AI");
            await Expect(reset).ToBeEnabledAsync();
            await AssertNoChangedSectionsAsync(editor);
            await AssertStableLayoutAsync(editor, stableEditorHeight);
        }

        var healthyTitle = session.Lock(script.GetLockName(1, 0));
        var healthyIngredients = session.Lock(script.GetLockName(1, 1));
        var healthyComplete = session.Lock(script.GetLockName(1, 2));
        await using (healthyTitle)
        await using (healthyIngredients)
        await using (healthyComplete)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Make it healthier", Exact = true }).ClickAsync();

            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Make the recipe healthier with more vegetables.");
            await Expect(editor.Locator("input[type='text']").First)
                .ToHaveValueAsync("Healthy Italian Garden Pasta");
            await AssertChangedSectionAsync(editor, "title");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await AssertCheckpointAsync(checkpoint, "healthy-title");

            await healthyTitle.ReleaseAsync();

            await AssertChangedSectionAsync(editor, "ingredients");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await Expect(editor.Locator(".ingredient-row").First
                .Locator(".ingredient-row__name")).ToHaveValueAsync("Whole-Wheat Spaghetti");
            await AssertCheckpointAsync(checkpoint, "healthy-ingredients");

            await healthyIngredients.ReleaseAsync();

            await AssertChangedSectionAsync(editor, "instructions");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await Expect(editor.Locator(".recipe-editor__instruction input").Nth(0))
                .ToHaveValueAsync("Cook whole-wheat spaghetti until al dente");
            await AssertCheckpointAsync(checkpoint, "healthy-complete");

            await healthyComplete.ReleaseAsync();
            await Expect(checkpoint).ToHaveCountAsync(0);
            await AssertNoChangedSectionsAsync(editor);
            await AssertStableLayoutAsync(editor, stableEditorHeight);
        }

        var improveIngredients = session.Lock(script.GetLockName(2, 0));
        var improveTitle = session.Lock(script.GetLockName(2, 1));
        var improveComplete = session.Lock(script.GetLockName(2, 2));
        await using (improveIngredients)
        await using (improveTitle)
        await using (improveComplete)
        {
            await improve.ClickAsync();

            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--user .sc-ai-message__content"))
                .ToHaveTextAsync("Improve the recipe");
            await Expect(improve).ToBeDisabledAsync();
            await Expect(improve).ToHaveTextAsync("Please Wait...");
            await Expect(editor.Locator(".ingredient-row")).ToHaveCountAsync(5);
            await Expect(editor.Locator(".ingredient-row").Last
                .Locator(".ingredient-row__name")).ToHaveValueAsync("Fresh Basil");
            await AssertChangedSectionAsync(editor, "ingredients");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await AssertCheckpointAsync(checkpoint, "improve-ingredients");

            await improveIngredients.ReleaseAsync();

            await Expect(editor.Locator("input[type='text']").First)
                .ToHaveValueAsync("Herbed Healthy Italian Garden Pasta");
            await AssertChangedSectionAsync(editor, "title");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await AssertCheckpointAsync(checkpoint, "improve-title");

            await improveTitle.ReleaseAsync();

            await Expect(editor.Locator(".recipe-editor__instruction input").Last)
                .ToHaveValueAsync("Finish with fresh basil");
            await AssertChangedSectionAsync(editor, "instructions");
            await AssertStableLayoutAsync(editor, stableEditorHeight);
            await Expect(scenario.Locator(".sc-ai-turn").Last
                .Locator(".sc-ai-message--assistant .sc-ai-message__content"))
                .ToHaveTextAsync(
                    "I added fresh basil and a brighter finish to improve the recipe.");
            await AssertCheckpointAsync(checkpoint, "improve-complete");

            await improveComplete.ReleaseAsync();

            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(improve).ToBeEnabledAsync();
            await Expect(improve).ToHaveTextAsync("Improve with AI");
            await Expect(send).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
            await AssertNoChangedSectionsAsync(editor);
            await AssertStableLayoutAsync(editor, stableEditorHeight);
        }

        await reset.ClickAsync();
        await AssertRecipeAsync(
            editor,
            "Make Your Recipe",
            "Intermediate",
            "45 min",
            [],
            [
                ("\U0001F955", "Carrots", "3 large, grated"),
                ("\U0001F33E", "All-Purpose Flour", "2 cups"),
            ],
            ["Preheat oven to 350\u00b0F (175\u00b0C)"]);
        await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(0);

        await CustomizeRecipeAsync(editor);
        session.ResetReplay();
        var resetFrames = Enumerable.Range(0, 4)
            .Select(index => session.Lock(script.GetLockName(0, index)))
            .ToArray();
        await using (resetFrames[0])
        await using (resetFrames[1])
        await using (resetFrames[2])
        await using (resetFrames[3])
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Create Italian recipe", Exact = true }).ClickAsync();
            await AssertCheckpointAsync(checkpoint, "italian-title");
            await resetFrames[0].ReleaseAsync();
            await AssertCheckpointAsync(checkpoint, "italian-details");
            await resetFrames[1].ReleaseAsync();
            await AssertCheckpointAsync(checkpoint, "italian-ingredients");
            await resetFrames[2].ReleaseAsync();
            await AssertCheckpointAsync(checkpoint, "italian-complete");
            await resetFrames[3].ReleaseAsync();
            await Expect(checkpoint).ToHaveCountAsync(0);
            await AssertItalianRecipeAsync(editor);
            await Expect(send).ToBeEnabledAsync();
        }
    }

    private static async Task AssertSuggestionsAsync(ILocator scenario)
    {
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Create Italian recipe", Exact = true })).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Make it healthier", Exact = true })).ToBeEnabledAsync();
        await Expect(scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Suggest variations", Exact = true })).ToBeEnabledAsync();
    }

    private static async Task CustomizeRecipeAsync(ILocator editor)
    {
        await editor.Locator("input[type='text']").First.FillAsync("Sunday Garden Pasta");
        await editor.Locator(".recipe-editor__row select").Nth(0).SelectOptionAsync("Beginner");
        await editor.Locator(".recipe-editor__row select").Nth(1).SelectOptionAsync("30 min");
        await editor.GetByLabel("High Protein", new() { Exact = true }).CheckAsync();

        var ingredients = editor.Locator(".ingredient-row");
        await ingredients.Nth(0).Locator(".ingredient-row__name").FillAsync("Zucchini");
        await ingredients.Nth(0).Locator(".ingredient-row__amount").FillAsync("2, sliced");
        await ingredients.Nth(1).Locator(".recipe-editor__remove-btn").ClickAsync();
        await editor.GetByRole(
            AriaRole.Button,
            new() { Name = "+ Add Ingredient", Exact = true }).ClickAsync();
        await ingredients.Nth(1).Locator(".ingredient-row__icon").FillAsync("\U0001F345");
        await ingredients.Nth(1).Locator(".ingredient-row__name").FillAsync("Tomatoes");
        await ingredients.Nth(1).Locator(".ingredient-row__amount").FillAsync("4, chopped");

        await editor.GetByRole(
            AriaRole.Button,
            new() { Name = "+ Add Step", Exact = true }).ClickAsync();
        await editor.Locator(".recipe-editor__instruction input").Nth(1)
            .FillAsync("Serve immediately");
    }

    private static Task AssertCustomizedRecipeAsync(ILocator editor)
        => AssertRecipeAsync(
            editor,
            "Sunday Garden Pasta",
            "Beginner",
            "30 min",
            ["High Protein"],
            [
                ("\U0001F955", "Zucchini", "2, sliced"),
                ("\U0001F345", "Tomatoes", "4, chopped"),
            ],
            ["Preheat oven to 350\u00b0F (175\u00b0C)", "Serve immediately"]);

    private static Task AssertItalianRecipeAsync(ILocator editor)
        => AssertRecipeAsync(
            editor,
            "Italian Garden Pasta",
            "Intermediate",
            "30 min",
            ["High Protein"],
            [
                ("\U0001F35D", "Spaghetti", "400g"),
                ("\U0001F952", "Zucchini", "2, sliced"),
                ("\U0001F345", "Tomatoes", "4, chopped"),
                ("\U0001F9C0", "Ricotta", "1 cup"),
            ],
            [
                "Cook spaghetti until al dente",
                "Saut\u00e9 zucchini and tomatoes until tender",
                "Toss with ricotta and serve immediately",
            ]);

    private static async Task AssertRecipeAsync(
        ILocator editor,
        string title,
        string skillLevel,
        string cookingTime,
        string[] preferences,
        (string Icon, string Name, string Amount)[] ingredients,
        string[] instructions)
    {
        await Expect(editor.Locator("input[type='text']").First).ToHaveValueAsync(title);
        await Expect(editor.Locator(".recipe-editor__row select").Nth(0))
            .ToHaveValueAsync(skillLevel);
        await Expect(editor.Locator(".recipe-editor__row select").Nth(1))
            .ToHaveValueAsync(cookingTime);

        var preferenceInputs = editor.Locator(".recipe-editor__checkbox input");
        var allPreferences = new[]
        {
            "Vegetarian", "Vegan", "Gluten-Free", "High Protein", "Low Carb", "Spicy",
        };
        for (var index = 0; index < allPreferences.Length; index++)
        {
            if (preferences.Contains(allPreferences[index], StringComparer.Ordinal))
            {
                await Expect(preferenceInputs.Nth(index)).ToBeCheckedAsync();
            }
            else
            {
                await Expect(preferenceInputs.Nth(index)).Not.ToBeCheckedAsync();
            }
        }

        var ingredientRows = editor.Locator(".ingredient-row");
        await Expect(ingredientRows).ToHaveCountAsync(ingredients.Length);
        for (var index = 0; index < ingredients.Length; index++)
        {
            var row = ingredientRows.Nth(index);
            await Expect(row.Locator(".ingredient-row__icon")).ToHaveValueAsync(ingredients[index].Icon);
            await Expect(row.Locator(".ingredient-row__name")).ToHaveValueAsync(ingredients[index].Name);
            await Expect(row.Locator(".ingredient-row__amount")).ToHaveValueAsync(ingredients[index].Amount);
        }

        var instructionInputs = editor.Locator(".recipe-editor__instruction input");
        await Expect(instructionInputs).ToHaveCountAsync(instructions.Length);
        for (var index = 0; index < instructions.Length; index++)
        {
            await Expect(instructionInputs.Nth(index)).ToHaveValueAsync(instructions[index]);
        }
    }

    private static async Task AssertCheckpointAsync(ILocator checkpoint, string name)
    {
        await Expect(checkpoint).ToHaveAttributeAsync("data-replay-checkpoint", name);
    }

    private static async Task AssertChangedSectionAsync(ILocator editor, string section)
    {
        var changed = editor.Locator(".recipe-editor__section--changed");
        await Expect(changed).ToHaveCountAsync(1);
        await Expect(changed).ToHaveAttributeAsync("data-section", section);
        await Expect(changed).ToHaveAttributeAsync("data-agent-changed", "true");
    }

    private static async Task AssertNoChangedSectionsAsync(ILocator editor)
    {
        await Expect(editor.Locator(".recipe-editor__section--changed")).ToHaveCountAsync(0);
        await Expect(editor.Locator(".recipe-editor__section[data-agent-changed='true']"))
            .ToHaveCountAsync(0);
    }

    private static Task<double> GetEditorHeightAsync(ILocator editor)
        => editor.EvaluateAsync<double>("element => element.getBoundingClientRect().height");

    private static async Task AssertStableLayoutAsync(ILocator editor, double expectedHeight)
    {
        var actualHeight = await GetEditorHeightAsync(editor);
        Assert.AreEqual(expectedHeight, actualHeight, 0.5);
    }
}
