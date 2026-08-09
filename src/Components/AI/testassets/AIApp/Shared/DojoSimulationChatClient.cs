// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AIApp.Components.Scenarios.AgenticGenerativeUI;
using AIApp.Components.Scenarios.PredictiveStateUpdates;
using AIApp.Components.Scenarios.SharedState;
using Microsoft.Extensions.AI;

namespace AIApp.Shared;

internal interface IDojoSimulationDelay
{
    Task DelayAsync(CancellationToken cancellationToken);
}

internal sealed class DojoSimulationDelay(TimeSpan delay) : IDojoSimulationDelay
{
    public Task DelayAsync(CancellationToken cancellationToken)
        => Task.Delay(delay, cancellationToken);
}

internal sealed class DojoSimulationChatClient(IDojoSimulationDelay delay) : IChatClient
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new(AIJsonUtilities.DefaultOptions);

    private int _confirmationIndex;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var toolNames = options?.Tools?
            .Select(tool => tool.Name)
            .ToHashSet(StringComparer.Ordinal) ?? [];

        if (toolNames.Contains("create_plan") && toolNames.Contains("update_plan_step"))
        {
            await foreach (var update in SimulateAgenticAsync(
                GetLastUserMessage(messageList),
                cancellationToken))
            {
                yield return update;
            }
        }
        else if (toolNames.Contains("generate_recipe"))
        {
            await foreach (var update in SimulateSharedStateAsync(
                GetLastUserMessage(messageList),
                GetState<RecipeState>(options),
                cancellationToken))
            {
                yield return update;
            }
        }
        else if (toolNames.Contains("write_document_local") &&
            toolNames.Contains("confirm_changes"))
        {
            await foreach (var update in SimulatePredictiveStateAsync(
                messageList,
                GetLastUserMessage(messageList),
                GetState<DocumentState>(options),
                cancellationToken))
            {
                yield return update;
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"The dojo simulator does not recognize tools [{string.Join(", ", toolNames)}].");
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

    private async IAsyncEnumerable<ChatResponseUpdate> SimulateAgenticAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var steps = GetPlanSteps(prompt);
        yield return CreateStateUpdate(new PlanState
        {
            Steps = [.. steps.Select(description => new PlanStep { Description = description })],
        });

        for (var index = 0; index < steps.Count; index++)
        {
            await delay.DelayAsync(cancellationToken);
            yield return CreateStateUpdate(new PlanState
            {
                Steps =
                [
                    .. steps.Select((description, stepIndex) => new PlanStep
                    {
                        Description = description,
                        Status = stepIndex <= index ? "completed" : "pending",
                    }),
                ],
            });
        }

        await delay.DelayAsync(cancellationToken);
        yield return CreateTextUpdate(
            $"All {steps.Count} steps in the " +
            $"{(prompt.Contains("pizza", StringComparison.OrdinalIgnoreCase) ? "pizza" : "Mars mission")} " +
            "plan are complete.");
    }

    private async IAsyncEnumerable<ChatResponseUpdate> SimulateSharedStateAsync(
        string prompt,
        RecipeState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var current = CloneRecipe(currentState.Recipe);
        var target = CreateTargetRecipe(prompt, current);

        current = current with
        {
            Title = target.Title,
            SkillLevel = target.SkillLevel,
            CookingTime = target.CookingTime,
        };
        yield return CreateStateUpdate(new RecipeState { Recipe = current });

        await delay.DelayAsync(cancellationToken);
        current = current with
        {
            SpecialPreferences = [.. target.SpecialPreferences],
        };
        yield return CreateStateUpdate(new RecipeState { Recipe = current });

        await delay.DelayAsync(cancellationToken);
        current = current with
        {
            Ingredients = [.. target.Ingredients.Select(CloneIngredient)],
        };
        yield return CreateStateUpdate(new RecipeState { Recipe = current });

        await delay.DelayAsync(cancellationToken);
        current = current with
        {
            Instructions = [.. target.Instructions],
        };
        yield return CreateStateUpdate(new RecipeState { Recipe = current });

        await delay.DelayAsync(cancellationToken);
        yield return CreateTextUpdate(GetRecipeAcknowledgement(prompt));
    }

    private async IAsyncEnumerable<ChatResponseUpdate> SimulatePredictiveStateAsync(
        IReadOnlyList<ChatMessage> messages,
        string prompt,
        DocumentState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var lastUserMessageIndex = Enumerable.Range(0, messages.Count)
            .Last(index => messages[index].Role == ChatRole.User);
        var currentRunContents = messages
            .Skip(lastUserMessageIndex + 1)
            .SelectMany(message => message.Contents)
            .ToList();
        var confirmationCallIds = currentRunContents
            .OfType<FunctionCallContent>()
            .Where(call => call.Name == "confirm_changes")
            .Select(call => call.CallId)
            .ToHashSet(StringComparer.Ordinal);
        var result = currentRunContents
            .OfType<FunctionResultContent>()
            .LastOrDefault(result => confirmationCallIds.Contains(result.CallId));
        if (result is not null)
        {
            var rejected = result.Result?.ToString()?.Contains(
                "rejected",
                StringComparison.OrdinalIgnoreCase) == true;
            yield return CreateTextUpdate(
                rejected
                    ? "I left the document unchanged."
                    : "The document changes are ready.");
            yield break;
        }

        var states = CreateDocumentStates(prompt, currentState.Document);
        foreach (var state in states)
        {
            yield return CreateStateUpdate(new DocumentState { Document = state });
            await delay.DelayAsync(cancellationToken);
        }

        var callId = $"dojo-confirm-{Interlocked.Increment(ref _confirmationIndex)}";
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new FunctionCallContent(callId, "confirm_changes", arguments: null)],
            FinishReason = ChatFinishReason.ToolCalls,
        };
    }

    private static IReadOnlyList<string> GetPlanSteps(string prompt)
    {
        if (prompt.Contains("mars", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Develop a comprehensive mission plan, detailing objectives, budget, and timeline.",
                "Design and test a spacecraft capable of transporting humans and cargo to Mars.",
                "Select and train astronaut crew for the mission.",
                "Establish communication systems and infrastructure for Mars exploration.",
                "Launch the spacecraft and execute the mission to Mars.",
            ];
        }

        if (prompt.Contains("pizza", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Choose the pizza style and menu.",
                "Prepare the dough ingredients.",
                "Mix and knead the dough.",
                "Let the dough rise.",
                "Prepare the sauce.",
                "Prepare the toppings.",
                "Shape the pizza base.",
                "Assemble the pizza.",
                "Bake until crisp and golden.",
                "Slice and serve the pizza.",
            ];
        }

        throw new InvalidOperationException($"Unsupported agentic planning prompt: '{prompt}'.");
    }

    private static Recipe CreateTargetRecipe(string prompt, Recipe current)
    {
        if (prompt.Contains("Italian pasta", StringComparison.OrdinalIgnoreCase))
        {
            return new Recipe
            {
                Title = "Italian Garden Pasta",
                SkillLevel = "Intermediate",
                CookingTime = "30 min",
                SpecialPreferences = ["Vegetarian"],
                Ingredients =
                [
                    new() { Icon = "🍝", Name = "Pasta", Amount = "12 oz" },
                    new() { Icon = "🍅", Name = "Tomatoes", Amount = "4, chopped" },
                    new() { Icon = "🫒", Name = "Olive Oil", Amount = "2 tbsp" },
                ],
                Instructions =
                [
                    "Cook the pasta until al dente.",
                    "Sauté the tomatoes in olive oil.",
                    "Toss the pasta with the garden sauce and serve.",
                ],
            };
        }

        if (prompt.Contains("healthier", StringComparison.OrdinalIgnoreCase))
        {
            var title = current.Title.StartsWith("Healthy ", StringComparison.Ordinal)
                ? current.Title
                : $"Healthy {current.Title}";
            return current with
            {
                Title = title,
                CookingTime = "30 min",
                SpecialPreferences = AddDistinct(current.SpecialPreferences, "Vegetarian"),
                Ingredients =
                [
                    .. current.Ingredients.Select(CloneIngredient),
                    new() { Icon = "🥒", Name = "Zucchini", Amount = "2, sliced" },
                    new() { Icon = "🥬", Name = "Spinach", Amount = "2 cups" },
                ],
                Instructions =
                [
                    .. current.Instructions,
                    "Fold in the fresh vegetables until just tender.",
                ],
            };
        }

        if (prompt.Contains("variations", StringComparison.OrdinalIgnoreCase))
        {
            var title = current.Title.StartsWith("Creative ", StringComparison.Ordinal)
                ? current.Title
                : $"Creative {current.Title}";
            return current with
            {
                Title = title,
                Ingredients =
                [
                    .. current.Ingredients.Select(CloneIngredient),
                    new() { Icon = "🍄", Name = "Wild Mushrooms", Amount = "1 cup" },
                ],
                Instructions =
                [
                    .. current.Instructions,
                    "Try the mushroom variation or substitute seasonal vegetables.",
                ],
            };
        }

        if (string.Equals(prompt, "Improve the recipe", StringComparison.OrdinalIgnoreCase))
        {
            var title = current.Title == "Make Your Recipe"
                ? "Herbed Garden Vegetable Bake"
                : current.Title.StartsWith("Herbed ", StringComparison.Ordinal)
                    ? current.Title
                    : $"Herbed {current.Title}";
            return current with
            {
                Title = title,
                SkillLevel = "Beginner",
                CookingTime = "30 min",
                SpecialPreferences = AddDistinct(current.SpecialPreferences, "Vegetarian"),
                Ingredients =
                [
                    .. current.Ingredients.Select(CloneIngredient),
                    new() { Icon = "🌿", Name = "Fresh Basil", Amount = "1 handful" },
                ],
                Instructions =
                [
                    .. current.Instructions,
                    "Finish with fresh basil before serving.",
                ],
            };
        }

        throw new InvalidOperationException($"Unsupported shared-state prompt: '{prompt}'.");
    }

    private static IReadOnlyList<string> CreateDocumentStates(string prompt, string currentDocument)
    {
        if (prompt.Contains("Candy Beard", StringComparison.OrdinalIgnoreCase))
        {
            const string title = "# Candy Beard's Voyage";
            const string opening =
                "# Candy Beard's Voyage\n\n" +
                "Candy Beard sailed from Gumdrop Harbor in search of the Sugar Star.";
            return
            [
                title,
                opening,
                opening +
                    "\n\nWhen dark clouds gathered, the crew shared their courage and found the way home.",
            ];
        }

        if (prompt.Contains("Luna", StringComparison.OrdinalIgnoreCase))
        {
            const string title = "# Luna and the Moonlit Reef";
            const string opening =
                "# Luna and the Moonlit Reef\n\n" +
                "Luna followed a silver current beyond the coral gardens.";
            return
            [
                title,
                opening,
                opening +
                    "\n\nShe returned at dawn with a pearl that glowed like the moon.",
            ];
        }

        if (prompt.Contains("Courage", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = string.IsNullOrWhiteSpace(currentDocument)
                ? "# Courage Joins the Voyage"
                : currentDocument.TrimEnd();
            return
            [
                $"{prefix}\n\nCourage joined the crew",
                $"{prefix}\n\nCourage joined the crew and offered to guide them through the storm.",
            ];
        }

        throw new InvalidOperationException($"Unsupported predictive-state prompt: '{prompt}'.");
    }

    private static string GetLastUserMessage(IReadOnlyList<ChatMessage> messages)
        => messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text
            ?? throw new InvalidOperationException("The dojo simulator requires a user message.");

    private TState GetState<TState>(ChatOptions? options)
        where TState : new()
    {
        var rawState = options?.RawRepresentationFactory?.Invoke(this);
        return rawState switch
        {
            null => new TState(),
            TState state => state,
            JsonElement json => json.Deserialize<TState>(_jsonOptions)
                ?? throw new InvalidOperationException($"Could not decode {typeof(TState).Name}."),
            _ => JsonSerializer.Deserialize<TState>(
                    JsonSerializer.Serialize(rawState, _jsonOptions),
                    _jsonOptions)
                ?? throw new InvalidOperationException($"Could not decode {typeof(TState).Name}."),
        };
    }

    private static ChatResponseUpdate CreateStateUpdate<TState>(TState state)
        => new()
        {
            Role = ChatRole.Assistant,
            RawRepresentation = JsonSerializer.SerializeToElement(state, _jsonOptions),
        };

    private static ChatResponseUpdate CreateTextUpdate(string text)
        => new(ChatRole.Assistant, text)
        {
            FinishReason = ChatFinishReason.Stop,
        };

    private static Recipe CloneRecipe(Recipe recipe)
        => recipe with
        {
            SpecialPreferences = [.. recipe.SpecialPreferences],
            Ingredients = [.. recipe.Ingredients.Select(CloneIngredient)],
            Instructions = [.. recipe.Instructions],
        };

    private static Ingredient CloneIngredient(Ingredient ingredient)
        => ingredient with { };

    private static List<string> AddDistinct(IEnumerable<string> values, string value)
        => [.. values.Append(value).Distinct(StringComparer.Ordinal)];

    private static string GetRecipeAcknowledgement(string prompt)
    {
        if (string.Equals(prompt, "Improve the recipe", StringComparison.OrdinalIgnoreCase))
        {
            return "I improved the recipe with fresh herbs and clearer steps.";
        }

        if (prompt.Contains("healthier", StringComparison.OrdinalIgnoreCase))
        {
            return "I made the recipe healthier with more vegetables.";
        }

        if (prompt.Contains("variations", StringComparison.OrdinalIgnoreCase))
        {
            return "I added a creative seasonal variation.";
        }

        return "Your Italian pasta recipe is ready.";
    }
}
