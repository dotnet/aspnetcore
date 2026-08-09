// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using AIApp.Components.Scenarios.AgenticGenerativeUI;
using AIApp.Components.Scenarios.PredictiveStateUpdates;
using AIApp.Components.Scenarios.SharedState;
using AIApp.Shared;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[TestClass]
public class DojoSimulationChatClientTests
{
    [TestMethod]
    public async Task RoutesScenariosByToolsWithoutCallOrdering()
    {
        var delay = new RecordingDelay();
        using var client = new DojoSimulationChatClient(delay);
        var recipe = CreateInitialRecipe();

        var improved = await CollectAsync(
            client,
            "Improve the recipe",
            CreateOptions(recipe, "generate_recipe"));
        recipe = GetStates<RecipeState>(improved)[^1];
        Assert.AreEqual("Herbed Garden Vegetable Bake", recipe.Recipe.Title);
        Assert.AreEqual("Fresh Basil", recipe.Recipe.Ingredients[^1].Name);

        var plan = await CollectAsync(
            client,
            "Please build a plan to go to make pizza in 10 steps.",
            CreateOptions(state: null, "create_plan", "update_plan_step"));
        var planStates = GetStates<PlanState>(plan);
        Assert.HasCount(11, planStates);
        Assert.HasCount(10, planStates[^1].Steps);
        Assert.IsTrue(planStates[^1].Steps.All(step => step.Status == "completed"));

        var predictive = await CollectAsync(
            client,
            "Please write a story about a mermaid named Luna.",
            CreateOptions(new DocumentState(), "confirm_changes", "write_document_local"));
        var documentStates = GetStates<DocumentState>(predictive);
        Assert.HasCount(3, documentStates);
        StringAssert.Contains(documentStates[^1].Document, "Luna");
        Assert.AreEqual(
            "confirm_changes",
            predictive.SelectMany(update => update.Contents)
                .OfType<FunctionCallContent>()
                .Single()
                .Name);

        var healthier = await CollectAsync(
            client,
            "Make the recipe healthier with more vegetables.",
            CreateOptions(recipe, "generate_recipe"));
        recipe = GetStates<RecipeState>(healthier)[^1];
        StringAssert.StartsWith(recipe.Recipe.Title, "Healthy ");
        Assert.IsTrue(recipe.Recipe.Ingredients.Any(ingredient => ingredient.Name == "Zucchini"));

        Assert.IsGreaterThan(0, delay.Count);
    }

    [TestMethod]
    public async Task EmitsOrderedObservableStateSequences()
    {
        var delay = new RecordingDelay();
        using var client = new DojoSimulationChatClient(delay);

        var agentic = await CollectAsync(
            client,
            "Please build a plan to go to mars in 5 steps.",
            CreateOptions(state: null, "create_plan", "update_plan_step"));
        var planStates = GetStates<PlanState>(agentic);
        CollectionAssert.AreEqual(
            new[] { 0, 1, 2, 3, 4, 5 },
            planStates
                .Select(state => state.Steps.Count(step => step.Status == "completed"))
                .ToArray());
        Assert.AreEqual(
            "All 5 steps in the Mars mission plan are complete.",
            agentic[^1].Text);

        var predictive = await CollectAsync(
            client,
            "Please write a story about a pirate named Candy Beard.",
            CreateOptions(
                new DocumentState { Document = "# Harbor Notes" },
                "confirm_changes",
                "write_document_local"));
        var documentStates = GetStates<DocumentState>(predictive);
        Assert.HasCount(3, documentStates);
        StringAssert.Contains(documentStates[0].Document, "Candy Beard's Voyage");
        Assert.DoesNotContain(documentStates[0].Document, "Sugar Star");
        StringAssert.Contains(documentStates[1].Document, "Sugar Star");
        Assert.DoesNotContain(documentStates[1].Document, "dark clouds");
        StringAssert.Contains(documentStates[2].Document, "dark clouds");
        Assert.IsInstanceOfType<FunctionCallContent>(predictive[^1].Contents.Single());
    }

    [TestMethod]
    public async Task PredictiveContinuationUsesFunctionResultWithoutCallOrdering()
    {
        using var client = new DojoSimulationChatClient(new RecordingDelay());
        var options = CreateOptions(
            new DocumentState { Document = "# Current" },
            "confirm_changes",
            "write_document_local");
        var messages = new ChatMessage[]
        {
            new(ChatRole.User, "Please add a character named Courage."),
            new(
                ChatRole.Assistant,
                [new FunctionCallContent("confirmation", "confirm_changes", arguments: null)]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent("confirmation", "The user rejected the changes.")]),
        };

        var response = await client.GetResponseAsync(messages, options);

        Assert.AreEqual("I left the document unchanged.", response.Text);

        var nextRequest = messages.Append(
            new ChatMessage(ChatRole.User, "Please write a story about a mermaid named Luna."));
        var nextUpdates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(nextRequest, options))
        {
            nextUpdates.Add(update);
        }

        var nextStates = GetStates<DocumentState>(nextUpdates);
        Assert.HasCount(
            3,
            nextStates,
            $"Received {nextUpdates.Count} updates: " +
            string.Join(", ", nextUpdates.Select(update =>
                $"{update.Text ?? "<no text>"}/{update.RawRepresentation?.GetType().Name ?? "<no state>"}")));
        Assert.AreEqual(
            "confirm_changes",
            nextUpdates.SelectMany(update => update.Contents)
                .OfType<FunctionCallContent>()
                .Single()
                .Name);
    }

    [TestMethod]
    public async Task CancellationStopsBetweenVisibleFrames()
    {
        using var client = new DojoSimulationChatClient(new CancellationDelay());
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Please build a plan to go to mars in 5 steps.")],
            CreateOptions(state: null, "create_plan", "update_plan_step"),
            cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.IsInstanceOfType<JsonElement>(enumerator.Current.RawRepresentation);

        var nextFrame = enumerator.MoveNextAsync().AsTask();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => nextFrame);
    }

    private static async Task<List<ChatResponseUpdate>> CollectAsync(
        IChatClient client,
        string prompt,
        ChatOptions options)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            options))
        {
            updates.Add(update);
        }

        return updates;
    }

    private static ChatOptions CreateOptions(object? state, params string[] toolNames)
        => new()
        {
            Tools =
            [
                .. toolNames.Select(name =>
                    AIFunctionFactory.Create(() => "simulated", name: name)),
            ],
            RawRepresentationFactory = state is null ? null : _ => state,
        };

    private static List<TState> GetStates<TState>(IEnumerable<ChatResponseUpdate> updates)
        => updates
            .Where(update => update.RawRepresentation is JsonElement)
            .Select(update => ((JsonElement)update.RawRepresentation!).Deserialize<TState>(
                AIJsonUtilities.DefaultOptions)!)
            .ToList();

    private static RecipeState CreateInitialRecipe()
        => new()
        {
            Recipe = new Recipe
            {
                Title = "Make Your Recipe",
                SkillLevel = "Intermediate",
                CookingTime = "45 min",
                Ingredients =
                [
                    new() { Icon = "🥕", Name = "Carrots", Amount = "3 large, grated" },
                    new() { Icon = "🌾", Name = "All-Purpose Flour", Amount = "2 cups" },
                ],
                Instructions = ["Preheat oven to 350°F (175°C)"],
            },
        };

    private sealed class RecordingDelay : IDojoSimulationDelay
    {
        public int Count { get; private set; }

        public Task DelayAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationDelay : IDojoSimulationDelay
    {
        public Task DelayAsync(CancellationToken cancellationToken)
            => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
