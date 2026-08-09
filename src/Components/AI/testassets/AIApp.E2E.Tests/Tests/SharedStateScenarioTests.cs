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
    public async Task RecipeEditorAndAgent_SynchronizeExactStateInBothDirections()
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

        await AssertRecipeAsync(
            editor,
            "Make Your Recipe",
            "Intermediate",
            "45 min",
            ["Vegetarian"],
            [
                ("\U0001F955", "Carrots", "3 large, grated"),
                ("\U0001F33E", "All-Purpose Flour", "2 cups"),
            ],
            ["Preheat oven to 350\u00b0F (175\u00b0C)"]);

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

        await AssertRecipeAsync(
            editor,
            "Sunday Garden Pasta",
            "Beginner",
            "30 min",
            ["Vegetarian", "High Protein"],
            [
                ("\U0001F955", "Zucchini", "2, sliced"),
                ("\U0001F345", "Tomatoes", "4, chopped"),
            ],
            ["Preheat oven to 350\u00b0F (175\u00b0C)", "Serve immediately"]);

        var italianTitle = session.Lock(script.GetLockName(0, 0));
        var italianIngredients = session.Lock(script.GetLockName(0, 1));
        var italianComplete = session.Lock(script.GetLockName(0, 2));
        await using (italianTitle)
        await using (italianIngredients)
        await using (italianComplete)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Create Italian recipe", Exact = true }).ClickAsync();

            await AssertRecipeAsync(
                editor,
                "Italian Garden Pasta",
                "Beginner",
                "30 min",
                ["Vegetarian", "High Protein"],
                [
                    ("\U0001F955", "Zucchini", "2, sliced"),
                    ("\U0001F345", "Tomatoes", "4, chopped"),
                ],
                ["Preheat oven to 350\u00b0F (175\u00b0C)", "Serve immediately"]);
            await AssertCheckpointAsync(checkpoint, "italian-title");
            await Expect(send).ToBeDisabledAsync();

            await italianTitle.ReleaseAsync();

            await AssertRecipeAsync(
                editor,
                "Italian Garden Pasta",
                "Beginner",
                "30 min",
                ["Vegetarian", "High Protein"],
                [
                    ("\U0001F35D", "Spaghetti", "400g"),
                    ("\U0001F952", "Zucchini", "2, sliced"),
                    ("\U0001F345", "Tomatoes", "4, chopped"),
                ],
                ["Preheat oven to 350\u00b0F (175\u00b0C)", "Serve immediately"]);
            await AssertCheckpointAsync(checkpoint, "italian-ingredients");
            await Expect(send).ToBeDisabledAsync();

            await italianIngredients.ReleaseAsync();

            await AssertRecipeAsync(
                editor,
                "Italian Garden Pasta",
                "Intermediate",
                "30 min",
                ["Vegetarian", "High Protein"],
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
            var firstTurn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(firstTurn.Locator(".sc-ai-message--assistant .sc-ai-message__content"))
                .ToHaveTextAsync(
                    "I've turned your recipe into an Italian garden pasta with spaghetti, " +
                    "zucchini, tomatoes, and a protein-rich ricotta sauce.");
            await AssertCheckpointAsync(checkpoint, "italian-complete");
            await Expect(send).ToBeDisabledAsync();

            await italianComplete.ReleaseAsync();

            await Expect(checkpoint).ToHaveCountAsync(0);
            await Expect(scenario.GetByRole(
                AriaRole.Status,
                new() { Name = "Agent is typing", Exact = true })).ToHaveCountAsync(0);
            await Expect(send).ToBeEnabledAsync();
            await Expect(input).ToHaveValueAsync("");
        }

        var healthySubstitution = session.Lock(script.GetLockName(1, 0));
        var healthyComplete = session.Lock(script.GetLockName(1, 1));
        await using (healthySubstitution)
        await using (healthyComplete)
        {
            await scenario.GetByRole(
                AriaRole.Button,
                new() { Name = "Make it healthier", Exact = true }).ClickAsync();

            await AssertRecipeAsync(
                editor,
                "Healthy Italian Garden Pasta",
                "Intermediate",
                "30 min",
                ["Vegetarian", "High Protein"],
                [
                    ("\U0001F35D", "Whole-Wheat Spaghetti", "400g"),
                    ("\U0001F952", "Zucchini", "2, sliced"),
                    ("\U0001F345", "Tomatoes", "4, chopped"),
                    ("\U0001F9C0", "Part-Skim Ricotta", "1 cup"),
                ],
                [
                    "Cook spaghetti until al dente",
                    "Saut\u00e9 zucchini and tomatoes until tender",
                    "Toss with ricotta and serve immediately",
                ]);
            await AssertCheckpointAsync(checkpoint, "healthy-substitution");
            await Expect(send).ToBeDisabledAsync();

            await healthySubstitution.ReleaseAsync();

            await AssertRecipeAsync(
                editor,
                "Healthy Italian Garden Pasta",
                "Intermediate",
                "30 min",
                ["Vegetarian", "High Protein"],
                [
                    ("\U0001F35D", "Whole-Wheat Spaghetti", "400g"),
                    ("\U0001F952", "Zucchini", "2, sliced"),
                    ("\U0001F345", "Tomatoes", "4, chopped"),
                    ("\U0001F9C0", "Part-Skim Ricotta", "1 cup"),
                ],
                [
                    "Cook whole-wheat spaghetti until al dente",
                    "Saut\u00e9 zucchini and tomatoes with olive oil",
                    "Toss with part-skim ricotta and serve immediately",
                ]);
            var finalTurn = scenario.Locator(".sc-ai-turn").Last;
            await Expect(finalTurn.Locator(".sc-ai-message--assistant .sc-ai-message__content"))
                .ToHaveTextAsync(
                    "I swapped in whole-wheat spaghetti and part-skim ricotta for a healthier, " +
                    "high-protein version.");
            await AssertCheckpointAsync(checkpoint, "healthy-complete");
            await Expect(send).ToBeDisabledAsync();

            await healthyComplete.ReleaseAsync();

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
        await AssertRecipeAsync(
            editor,
            "Make Your Recipe",
            "Intermediate",
            "45 min",
            ["Vegetarian"],
            [
                ("\U0001F955", "Carrots", "3 large, grated"),
                ("\U0001F33E", "All-Purpose Flour", "2 cups"),
            ],
            ["Preheat oven to 350\u00b0F (175\u00b0C)"]);
        await Expect(scenario.Locator(".sc-ai-turn")).ToHaveCountAsync(0);
    }

    private static async Task AssertRecipeAsync(
        ILocator editor,
        string title,
        string skillLevel,
        string cookingTime,
        string[] preferences,
        (string Icon, string Name, string Amount)[] ingredients,
        string[] instructions)
    {
        await Expect(editor.Locator("input[type='text']").First)
            .ToHaveValueAsync(title);
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
            await Expect(row.Locator(".ingredient-row__icon"))
                .ToHaveValueAsync(ingredients[index].Icon);
            await Expect(row.Locator(".ingredient-row__name"))
                .ToHaveValueAsync(ingredients[index].Name);
            await Expect(row.Locator(".ingredient-row__amount"))
                .ToHaveValueAsync(ingredients[index].Amount);
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
}
