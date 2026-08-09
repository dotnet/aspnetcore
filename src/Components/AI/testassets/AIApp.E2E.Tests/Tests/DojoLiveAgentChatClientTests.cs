// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AIApp.Components.Scenarios.AgenticGenerativeUI;
using AIApp.Components.Scenarios.PredictiveStateUpdates;
using AIApp.Components.Scenarios.SharedState;
using AIApp.Shared;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[TestClass]
public class DojoLiveAgentChatClientTests
{
    [TestMethod]
    public async Task RoutesPassThroughScenariosAndInjectsScenarioInstructions()
    {
        var model = new RecordingModelClient((messages, options, _) =>
        {
            Assert.AreEqual(ChatRole.System, messages[0].Role);
            Assert.IsNotEmpty(messages[0].Text);
            Assert.IsNull(options?.RawRepresentationFactory);
            return [new ChatResponseUpdate(ChatRole.Assistant, "model response")];
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());

        var scenarios = new[]
        {
            (Tool: "change_background", PromptFragment: "user's name is Bob"),
            (Tool: "get_weather", PromptFragment: "weather assistant"),
            (Tool: "generate_task_steps", PromptFragment: "exactly 5"),
            (Tool: "generate_haiku", PromptFragment: "exactly three Japanese"),
        };

        foreach (var scenario in scenarios)
        {
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, $"arbitrary request for {scenario.Tool}")],
                CreateOptions(state: null, scenario.Tool));

            Assert.AreEqual("model response", response.Text);
            StringAssert.Contains(model.Calls[^1].Messages[0].Text, scenario.PromptFragment);
        }
    }

    [TestMethod]
    public async Task RemovesProviderMetadataFromModelUpdates()
    {
        var providerRawRepresentation = new { Provider = "Azure OpenAI" };
        var content = new TextContent("safe content")
        {
            RawRepresentation = providerRawRepresentation,
            AdditionalProperties = new() { ["provider"] = "Azure OpenAI" },
        };
        var model = new RecordingModelClient((_, _, _) =>
        [
            new ChatResponseUpdate(ChatRole.Assistant, [content])
            {
                ModelId = "provider-deployment",
                RawRepresentation = providerRawRepresentation,
                AdditionalProperties = new() { ["usage"] = 42 },
            },
        ]);
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());

        var updates = await CollectAsync(
            client,
            [new ChatMessage(ChatRole.User, "Write a sonnet.")],
            CreateOptions(state: null, "change_background"));

        Assert.HasCount(1, updates);
        Assert.AreEqual("safe content", updates[0].Text);
        Assert.IsNull(updates[0].ModelId);
        Assert.IsNull(updates[0].RawRepresentation);
        Assert.IsNull(updates[0].AdditionalProperties);
        Assert.IsNull(updates[0].Contents[0].RawRepresentation);
        Assert.IsNull(updates[0].Contents[0].AdditionalProperties);
    }

    [TestMethod]
    public async Task AgenticMapsToolResultsToSnapshotAndDeltaEvents()
    {
        var invocation = 0;
        var model = new RecordingModelClient((_, _, _) =>
        {
            invocation++;
            return invocation switch
            {
                1 =>
                [
                    CreateFunctionCallUpdate(
                        "create",
                        "create_plan",
                        ("steps", new[] { "Research route", "Launch mission" })),
                ],
                2 =>
                [
                    CreateFunctionCallUpdate(
                        "update",
                        "update_plan_step",
                        ("index", 0),
                        ("status", "completed")),
                ],
                3 => [new ChatResponseUpdate(ChatRole.Assistant, "Stopped too early.")],
                _ => [new ChatResponseUpdate(ChatRole.Assistant, "Plan complete.")],
            };
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var options = CreateOptions(state: null, "create_plan", "update_plan_step");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Build an arbitrary two-step mission plan."),
        };

        var createUpdates = await CollectAsync(client, messages, options);
        var createCall = GetSingleCall(createUpdates);
        messages.Add(new ChatMessage(ChatRole.Assistant, [createCall]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(createCall.CallId, "created")]));

        var updateUpdates = await CollectAsync(client, messages, options);
        var snapshot = Assert.IsInstanceOfType<DojoStateSnapshotEvent>(
            updateUpdates[0].RawRepresentation);
        var plan = snapshot.Snapshot.Deserialize<PlanState>(AIJsonUtilities.DefaultOptions)!;
        Assert.HasCount(2, plan.Steps);
        Assert.IsTrue(plan.Steps.All(step => step.Status == "pending"));

        var updateCall = GetSingleCall(updateUpdates);
        messages.Add(new ChatMessage(ChatRole.Assistant, [updateCall]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(updateCall.CallId, "updated")]));

        var fallbackUpdates = await CollectAsync(client, messages, options);
        var delta = Assert.IsInstanceOfType<DojoStateDeltaEvent>(
            fallbackUpdates[0].RawRepresentation);
        var operation = delta.Delta.EnumerateArray().Single();
        Assert.AreEqual("replace", operation.GetProperty("op").GetString());
        Assert.AreEqual("/steps/0/status", operation.GetProperty("path").GetString());
        Assert.AreEqual("completed", operation.GetProperty("value").GetString());

        var fallbackCalls = fallbackUpdates
            .SelectMany(update => update.Contents)
            .OfType<FunctionCallContent>()
            .ToList();
        Assert.HasCount(
            1,
            fallbackCalls,
            string.Join(
                ", ",
                fallbackUpdates.Select(update =>
                    $"{update.Text ?? "<no text>"}/{update.RawRepresentation?.GetType().Name ?? "<no raw>"}")));
        var fallbackCall = fallbackCalls[0];
        Assert.AreEqual(1, Assert.IsInstanceOfType<int>(fallbackCall.Arguments!["index"]));
        Assert.AreEqual(
            "completed",
            fallbackCall.Arguments["status"]?.ToString());
        messages.Add(new ChatMessage(ChatRole.Assistant, [fallbackCall]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(fallbackCall.CallId, "updated")]));

        var finalUpdates = await CollectAsync(client, messages, options);
        var finalDelta = Assert.IsInstanceOfType<DojoStateDeltaEvent>(
            finalUpdates[0].RawRepresentation);
        Assert.AreEqual(
            "/steps/1/status",
            finalDelta.Delta.EnumerateArray().Single().GetProperty("path").GetString());
        Assert.AreEqual("Plan complete.", finalUpdates[^1].Text);
    }

    [TestMethod]
    public async Task HumanInTheLoopContinuationUsesOnlyCurrentSelectedSteps()
    {
        var model = new RecordingModelClient((_, _, _) =>
            throw new AssertFailedException("The confirmation continuation must not call the model."));
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var currentCall = new FunctionCallContent(
            "current",
            "generate_task_steps",
            arguments: null);
        var messages = new ChatMessage[]
        {
            new(ChatRole.User, "Old plan"),
            new(
                ChatRole.Assistant,
                [new FunctionCallContent("old", "generate_task_steps", arguments: null)]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent(
                    "old",
                    "The user selected the following steps: ignored old task")]),
            new(ChatRole.User, "Current arbitrary plan"),
            new(ChatRole.Assistant, [currentCall]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent(
                    currentCall.CallId,
                    "The user selected the following steps: task one, task three")]),
        };

        var response = await client.GetResponseAsync(
            messages,
            CreateOptions(state: null, "generate_task_steps"));

        Assert.AreEqual(
            "I'll move forward with the selected tasks: task one, task three.",
            response.Text);
        Assert.DoesNotContain(response.Text, "ignored old task");
    }

    [TestMethod]
    public async Task AgenticReplacesNonProgressingModelCallWithNextStep()
    {
        var invocation = 0;
        var model = new RecordingModelClient((_, _, _) =>
        {
            invocation++;
            return invocation == 1
                ?
                [
                    CreateFunctionCallUpdate(
                        "create",
                        "create_plan",
                        ("steps", new[] { "First", "Second" })),
                ]
                :
                [
                    CreateFunctionCallUpdate(
                        "duplicate-create",
                        "create_plan",
                        ("steps", new[] { "Replacement" })),
                ];
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var options = CreateOptions(state: null, "create_plan", "update_plan_step");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Create an arbitrary two-step plan."),
        };
        var initial = await CollectAsync(client, messages, options);
        var createCall = GetSingleCall(initial);
        messages.Add(new ChatMessage(ChatRole.Assistant, [createCall]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(createCall.CallId, "created")]));

        var continuation = await CollectAsync(client, messages, options);

        var nextCall = GetSingleCall(continuation);
        Assert.AreEqual("update_plan_step", nextCall.Name);
        Assert.AreEqual(0, Assert.IsInstanceOfType<int>(nextCall.Arguments!["index"]));
        Assert.AreEqual("completed", nextCall.Arguments["status"]?.ToString());
    }

    [TestMethod]
    public async Task AgenticCancellationDoesNotRetainUndeliveredPlan()
    {
        var model = new RecordingModelClient((_, _, _) =>
            [new ChatResponseUpdate(ChatRole.Assistant, "unused")]);
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var options = CreateOptions(state: null, "create_plan", "update_plan_step");
        var createCall = new FunctionCallContent(
            "create",
            "create_plan",
            new Dictionary<string, object?>
            {
                ["steps"] = JsonSerializer.SerializeToElement(
                    new[] { "First", "Second" },
                    AIJsonUtilities.DefaultOptions),
            });
        var messages = new ChatMessage[]
        {
            new(ChatRole.User, "Create a plan."),
            new(ChatRole.Assistant, [createCall]),
            new(ChatRole.Tool, [new FunctionResultContent(createCall.CallId, "created")]),
        };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync(
                messages,
                options,
                cancellation.Token))
            {
            }
        });

        var retry = await CollectAsync(client, messages, options);
        var stateEvent = Assert.IsInstanceOfType<DojoStateSnapshotEvent>(
            retry[0].RawRepresentation);
        Assert.HasCount(
            2,
            stateEvent.Snapshot.Deserialize<PlanState>(
                AIJsonUtilities.DefaultOptions)!.Steps);
    }

    [TestMethod]
    public async Task AgenticNewUserRequestDoesNotContinueInterruptedPlan()
    {
        var invocation = 0;
        var model = new RecordingModelClient((_, _, _) =>
        {
            invocation++;
            return invocation switch
            {
                1 =>
                [
                    CreateFunctionCallUpdate(
                        "old-create",
                        "create_plan",
                        ("steps", new[] { "Old first", "Old second" })),
                ],
                2 => [new ChatResponseUpdate(ChatRole.Assistant, "Old plan is ready.")],
                _ =>
                [
                    CreateFunctionCallUpdate(
                        "new-create",
                        "create_plan",
                        ("steps", new[] { "New only" })),
                ],
            };
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var options = CreateOptions(state: null, "create_plan", "update_plan_step");
        var oldMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Create the old plan."),
        };
        var initial = await CollectAsync(client, oldMessages, options);
        var oldCall = GetSingleCall(initial);
        oldMessages.Add(new ChatMessage(ChatRole.Assistant, [oldCall]));
        oldMessages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(oldCall.CallId, "created")]));
        await CollectAsync(client, oldMessages, options);

        var freshUpdates = await CollectAsync(
            client,
            [new ChatMessage(ChatRole.User, "Create a completely new plan.")],
            options);

        var freshCall = GetSingleCall(freshUpdates);
        Assert.AreEqual("create_plan", freshCall.Name);
        Assert.AreEqual("new-create", freshCall.CallId);
    }

    [TestMethod]
    public async Task SharedStateMapsCompleteRecipeAndSendsCurrentStateToModel()
    {
        var target = new Recipe
        {
            Title = "Garden Pasta",
            SkillLevel = "Intermediate",
            CookingTime = "30 min",
            SpecialPreferences = ["Vegetarian"],
            Ingredients = [new() { Icon = "🍅", Name = "Tomatoes", Amount = "4" }],
            Instructions = ["Cook and serve."],
        };
        var invocation = 0;
        var model = new RecordingModelClient((messages, options, _) =>
        {
            invocation++;
            if (invocation == 1)
            {
                StringAssert.Contains(messages[0].Text, "\"title\": \"Make Your Recipe\"");
                return
                [
                    CreateFunctionCallUpdate(
                        "recipe",
                        "generate_recipe",
                        ("recipe", target)),
                ];
            }

            StringAssert.Contains(messages[0].Text, "Do not call any tools");
            Assert.IsEmpty(options?.Tools ?? []);
            Assert.AreEqual(ChatToolMode.None, options?.ToolMode);
            return
            [
                new ChatResponseUpdate(ChatRole.Assistant, "Updated"),
                new ChatResponseUpdate(ChatRole.Assistant, " the recipe."),
            ];
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var initialState = CreateInitialRecipe();
        var options = CreateOptions(initialState, "generate_recipe");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Make this into an arbitrary tomato pasta."),
        };

        var callUpdates = await CollectAsync(client, messages, options);
        var call = GetSingleCall(callUpdates);
        messages.Add(new ChatMessage(ChatRole.Assistant, [call]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(call.CallId, new RecipeState { Recipe = target })]));

        var resultUpdates = await CollectAsync(client, messages, options);

        var stateEvent = Assert.IsInstanceOfType<DojoStateSnapshotEvent>(
            resultUpdates[0].RawRepresentation);
        var state = stateEvent.Snapshot.Deserialize<RecipeState>(AIJsonUtilities.DefaultOptions)!;
        Assert.AreEqual("Garden Pasta", state.Recipe.Title);
        Assert.HasCount(2, resultUpdates);
        Assert.AreEqual("Updated the recipe.", resultUpdates[^1].Text);
    }

    [TestMethod]
    public async Task SharedStateRejectsUnexpectedSummaryToolCallBeforePublishingState()
    {
        var invocation = 0;
        var model = new RecordingModelClient((_, _, _) =>
        {
            invocation++;
            return invocation == 1
                ?
                [
                    new ChatResponseUpdate(ChatRole.Assistant, "Created"),
                    CreateFunctionCallUpdate(
                        "duplicate",
                        "generate_recipe",
                        ("recipe", CreateInitialRecipe().Recipe)),
                ]
                : [new ChatResponseUpdate(ChatRole.Assistant, "Created the recipe.")];
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var recipe = CreateInitialRecipe().Recipe;
        var call = new FunctionCallContent(
            "recipe",
            "generate_recipe",
            new Dictionary<string, object?>
            {
                ["recipe"] = JsonSerializer.SerializeToElement(
                    recipe,
                    AIJsonUtilities.DefaultOptions),
            });
        var messages = new ChatMessage[]
        {
            new(ChatRole.User, "Create an Italian recipe."),
            new(ChatRole.Assistant, [call]),
            new(ChatRole.Tool, [new FunctionResultContent(call.CallId, "created")]),
        };
        var options = CreateOptions(CreateInitialRecipe(), "generate_recipe");
        var publishedUpdates = new List<ChatResponseUpdate>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync(messages, options))
            {
                publishedUpdates.Add(update);
            }
        });

        Assert.IsEmpty(publishedUpdates);
        var retry = await CollectAsync(client, messages, options);
        Assert.IsInstanceOfType<DojoStateSnapshotEvent>(retry[0].RawRepresentation);
        Assert.AreEqual("Created the recipe.", retry[^1].Text);
    }

    [TestMethod]
    public async Task SharedStateCancellationDoesNotConsumeUndeliveredRecipe()
    {
        var model = new RecordingModelClient((_, _, _) =>
            [new ChatResponseUpdate(ChatRole.Assistant, "Updated.")]);
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var recipe = CreateInitialRecipe().Recipe;
        var call = new FunctionCallContent(
            "recipe",
            "generate_recipe",
            new Dictionary<string, object?>
            {
                ["recipe"] = JsonSerializer.SerializeToElement(
                    recipe,
                    AIJsonUtilities.DefaultOptions),
            });
        var messages = new ChatMessage[]
        {
            new(ChatRole.User, "Update the recipe."),
            new(ChatRole.Assistant, [call]),
            new(ChatRole.Tool, [new FunctionResultContent(call.CallId, "updated")]),
        };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync(
                messages,
                CreateOptions(CreateInitialRecipe(), "generate_recipe"),
                cancellation.Token))
            {
            }
        });

        var retry = await CollectAsync(
            client,
            messages,
            CreateOptions(CreateInitialRecipe(), "generate_recipe"));
        Assert.IsInstanceOfType<DojoStateSnapshotEvent>(retry[0].RawRepresentation);
    }

    [TestMethod]
    public async Task PredictiveStreamsStateThenContinuesAfterCurrentConfirmation()
    {
        const string candidate = "# Harbor Log\n\nCandy Beard found a silver compass.";
        var invocation = 0;
        var model = new RecordingModelClient((messages, options, _) =>
        {
            invocation++;
            if (invocation == 1)
            {
                return
                [
                    CreateFunctionCallUpdate(
                        "write",
                        "write_document_local",
                        ("document", candidate)),
                ];
            }

            StringAssert.Contains(messages[0].Text, "Do not call any tools");
            Assert.IsEmpty(options?.Tools ?? []);
            Assert.IsTrue(messages
                .SelectMany(message => message.Contents)
                .OfType<FunctionCallContent>()
                .Any(call => call.Name == "write_document_local"));
            Assert.IsFalse(messages
                .SelectMany(message => message.Contents)
                .OfType<FunctionCallContent>()
                .Any(call => call.Name == "confirm_changes"));
            Assert.IsFalse(messages
                .Skip(1)
                .Any(message => message.Text.Contains("rejected", StringComparison.OrdinalIgnoreCase)));
            StringAssert.Contains(messages[^1].Text, "confirmed");
            return [new ChatResponseUpdate(ChatRole.Assistant, "The document is committed.")];
        });
        var delay = new ImmediateDelay();
        using var client = new DojoLiveAgentChatClient(model, delay);
        var options = CreateOptions(
            new DocumentState { Document = "# Existing" },
            "confirm_changes",
            "write_document_local");
        var oldConfirmation = new FunctionCallContent(
            "old-confirm",
            "confirm_changes",
            arguments: null);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Write an earlier document."),
            new(ChatRole.Assistant, [oldConfirmation]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent(oldConfirmation.CallId, "The user rejected the changes.")]),
            new(ChatRole.Assistant, "The earlier change was rejected."),
            new(ChatRole.User, "Write any short pirate story."),
        };

        var proposalUpdates = await CollectAsync(client, messages, options);

        var states = proposalUpdates
            .Select(update => update.RawRepresentation)
            .OfType<DojoStateSnapshotEvent>()
            .Select(update => update.Snapshot.Deserialize<DocumentState>(
                AIJsonUtilities.DefaultOptions)!)
            .ToList();
        Assert.IsGreaterThan(1, states.Count);
        Assert.AreEqual(candidate, states[^1].Document);
        Assert.IsTrue(states.Zip(states.Skip(1))
            .All(pair => pair.Second.Document.StartsWith(pair.First.Document, StringComparison.Ordinal)));
        Assert.AreEqual(states.Count - 1, delay.Count);
        Assert.IsFalse(proposalUpdates
            .SelectMany(update => update.Contents)
            .OfType<FunctionCallContent>()
            .Any(call => call.Name == "write_document_local"));

        var confirmCall = GetSingleCall(proposalUpdates);
        Assert.AreEqual("confirm_changes", confirmCall.Name);
        messages.Add(new ChatMessage(ChatRole.Assistant, [confirmCall]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(confirmCall.CallId, "The user confirmed the changes.")]));

        var continuation = await client.GetResponseAsync(messages, options);

        Assert.AreEqual("The document is committed.", continuation.Text);
    }

    [TestMethod]
    public async Task PredictiveLaterRunIgnoresRetainedConfirmationResults()
    {
        var model = new RecordingModelClient((_, _, _) =>
        [
            CreateFunctionCallUpdate(
                Guid.NewGuid().ToString("N"),
                "write_document_local",
                ("document", "# Fresh document")),
        ]);
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var options = CreateOptions(
            new DocumentState { Document = "# Current" },
            "confirm_changes",
            "write_document_local");
        var oldConfirmation = new FunctionCallContent("old-confirm", "confirm_changes", arguments: null);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Old request"),
            new(ChatRole.Assistant, [oldConfirmation]),
            new(
                ChatRole.Tool,
                [new FunctionResultContent(oldConfirmation.CallId, "The user rejected the changes.")]),
            new(ChatRole.User, "A completely different new request"),
        };

        var updates = await CollectAsync(client, messages, options);

        Assert.AreEqual(
            "confirm_changes",
            updates.SelectMany(update => update.Contents)
                .OfType<FunctionCallContent>()
                .Single()
                .Name);
        Assert.IsFalse(model.Calls[0].Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Any(result => result.CallId == oldConfirmation.CallId));
    }

    [TestMethod]
    public async Task PredictiveFailedContinuationRetainsPendingDocument()
    {
        var invocation = 0;
        var model = new RecordingModelClient((_, _, _) =>
        {
            invocation++;
            return invocation switch
            {
                1 =>
                [
                    CreateFunctionCallUpdate(
                        "write",
                        "write_document_local",
                        ("document", "# Candidate")),
                ],
                2 => throw new InvalidOperationException("Transient provider failure."),
                _ => [new ChatResponseUpdate(ChatRole.Assistant, "Confirmed on retry.")],
            };
        });
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        var options = CreateOptions(
            new DocumentState { Document = "# Existing" },
            "confirm_changes",
            "write_document_local");
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Write a candidate."),
        };
        var proposal = await CollectAsync(client, messages, options);
        var confirmation = GetSingleCall(proposal);
        messages.Add(new ChatMessage(ChatRole.Assistant, [confirmation]));
        messages.Add(new ChatMessage(
            ChatRole.Tool,
            [new FunctionResultContent(
                confirmation.CallId,
                "The user confirmed the changes.")]));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetResponseAsync(messages, options));
        messages.Add(new ChatMessage(ChatRole.User, "Write a candidate."));
        var retry = await client.GetResponseAsync(messages, options);

        Assert.AreEqual("Confirmed on retry.", retry.Text);
    }

    [TestMethod]
    public async Task PredictiveCancellationStopsBetweenStateFrames()
    {
        var model = new RecordingModelClient((_, _, _) =>
        [
            CreateFunctionCallUpdate(
                "write",
                "write_document_local",
                ("document", "# A document long enough for several frames")),
        ]);
        using var client = new DojoLiveAgentChatClient(model, new CancellationDelay());
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Write a document")],
            CreateOptions(
                new DocumentState(),
                "confirm_changes",
                "write_document_local"),
            cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.IsInstanceOfType<DojoStateSnapshotEvent>(
            enumerator.Current.RawRepresentation);
        var nextFrame = enumerator.MoveNextAsync().AsTask();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => nextFrame);
    }

    [TestMethod]
    public async Task PredictiveCancellationAfterFinalStateSuppressesConfirmation()
    {
        var model = new RecordingModelClient((_, _, _) =>
        [
            CreateFunctionCallUpdate(
                "write",
                "write_document_local",
                ("document", "short")),
        ]);
        using var client = new DojoLiveAgentChatClient(model, new ImmediateDelay());
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Write a short document")],
            CreateOptions(
                new DocumentState(),
                "confirm_changes",
                "write_document_local"),
            cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.IsTrue(await enumerator.MoveNextAsync());
        var stateEvent = Assert.IsInstanceOfType<DojoStateSnapshotEvent>(
            enumerator.Current.RawRepresentation);
        Assert.AreEqual(
            "short",
            stateEvent.Snapshot.Deserialize<DocumentState>(
                AIJsonUtilities.DefaultOptions)!.Document);
        Assert.IsNull(enumerator.Current.MessageId);
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => enumerator.MoveNextAsync().AsTask());
    }

    [TestMethod]
    public void StateEventsMatchAGUISerializationContract()
    {
        var snapshot = new DojoStateSnapshotEvent
        {
            Snapshot = JsonSerializer.SerializeToElement(new { value = 1 }),
        };
        var delta = new DojoStateDeltaEvent
        {
            Delta = JsonSerializer.SerializeToElement(
                new[] { new { op = "replace", path = "/value", value = 2 } }),
        };

        AssertEventShape(
            JsonSerializer.SerializeToElement(snapshot, AIJsonUtilities.DefaultOptions),
            "STATE_SNAPSHOT",
            "snapshot");
        AssertEventShape(
            JsonSerializer.SerializeToElement(delta, AIJsonUtilities.DefaultOptions),
            "STATE_DELTA",
            "delta");
    }

    private static void AssertEventShape(
        JsonElement serialized,
        string expectedType,
        string payloadProperty)
    {
        CollectionAssert.AreEqual(
            new[] { "type", payloadProperty },
            serialized.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(expectedType, serialized.GetProperty("type").GetString());
        Assert.IsFalse(serialized.TryGetProperty("timestamp", out _));
        Assert.IsFalse(serialized.TryGetProperty("rawEvent", out _));
    }

    private static ChatResponseUpdate CreateFunctionCallUpdate(
        string callId,
        string name,
        params (string Name, object? Value)[] arguments)
        => new()
        {
            Role = ChatRole.Assistant,
            Contents =
            [
                new FunctionCallContent(
                    callId,
                    name,
                    arguments.ToDictionary(
                        argument => argument.Name,
                        argument => (object?)JsonSerializer.SerializeToElement(
                            argument.Value,
                            AIJsonUtilities.DefaultOptions))),
            ],
            FinishReason = ChatFinishReason.ToolCalls,
        };

    private static FunctionCallContent GetSingleCall(
        IEnumerable<ChatResponseUpdate> updates)
        => updates.SelectMany(update => update.Contents)
            .OfType<FunctionCallContent>()
            .Single();

    private static ChatOptions CreateOptions(object? state, params string[] toolNames)
        => new()
        {
            Tools =
            [
                .. toolNames.Select(name =>
                    AIFunctionFactory.Create(() => "test result", name: name)),
            ],
            RawRepresentationFactory = state is null ? null : _ => state,
        };

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
                    new() { Icon = "🥕", Name = "Carrots", Amount = "3 large" },
                ],
                Instructions = ["Preheat the oven."],
            },
        };

    private static async Task<List<ChatResponseUpdate>> CollectAsync(
        IChatClient client,
        IEnumerable<ChatMessage> messages,
        ChatOptions options)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages, options))
        {
            updates.Add(update);
        }

        return updates;
    }

    private sealed class RecordingModelClient(
        Func<IReadOnlyList<ChatMessage>, ChatOptions?, CancellationToken, IReadOnlyList<ChatResponseUpdate>>
            responseFactory)
        : IChatClient
    {
        public List<ModelCall> Calls { get; } = [];

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var call = new ModelCall(messages.ToList(), options);
            Calls.Add(call);
            foreach (var update in responseFactory(call.Messages, options, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
                await Task.Yield();
            }
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => GetStreamingResponseAsync(messages, options, cancellationToken)
                .ToChatResponseAsync(cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed record ModelCall(
        IReadOnlyList<ChatMessage> Messages,
        ChatOptions? Options);

    private sealed class ImmediateDelay : IDojoLiveAgentDelay
    {
        public int Count { get; private set; }

        public Task DelayAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationDelay : IDojoLiveAgentDelay
    {
        public Task DelayAsync(CancellationToken cancellationToken)
            => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
