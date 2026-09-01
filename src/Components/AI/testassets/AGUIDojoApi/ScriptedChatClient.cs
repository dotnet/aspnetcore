// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AGUI.Server;
using AGUIDojoApi.SharedState;
using Microsoft.Extensions.AI;

namespace AGUIDojoApi;

// Local stand-in for a model, used when no live model is configured. It streams a canned
// answer one word at a time so the dojo can be exercised end to end (including incremental
// rendering) without any credentials.
internal sealed class ScriptedChatClient : IChatClient
{
    private static readonly TimeSpan ModelDelay = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan TokenDelay = TimeSpan.FromMilliseconds(60);
    private static readonly string[] MarsPlanSteps =
    [
        "Develop a comprehensive mission plan, detailing objectives, budget, and timeline.",
        "Design and test a spacecraft capable of transporting humans and cargo to Mars.",
        "Select and train astronaut crew for the mission.",
        "Establish communication systems and infrastructure for Mars exploration.",
        "Launch the spacecraft and execute the mission to Mars.",
    ];
    private static readonly string[] PizzaPlanSteps =
    [
        "Choose the pizza style and serving size.",
        "Gather flour, yeast, water, salt, and olive oil.",
        "Mix and knead the pizza dough.",
        "Let the dough rise until doubled in size.",
        "Prepare the tomato sauce.",
        "Slice and organize the toppings.",
        "Preheat the oven and baking surface.",
        "Shape the dough and add sauce and toppings.",
        "Bake the pizza until the crust is golden.",
        "Rest, slice, and serve the pizza.",
    ];

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        var prompt = messageList.LastOrDefault(message => message.Role == ChatRole.User)?.Text ?? string.Empty;
        var planSteps = prompt.Contains("pizza", StringComparison.OrdinalIgnoreCase)
            ? PizzaPlanSteps
            : MarsPlanSteps;

        if (messageList.LastOrDefault()?.Role == ChatRole.Tool)
        {
            var functionResult = messageList[^1].Contents
                .OfType<FunctionResultContent>()
                .FirstOrDefault();
            if (functionResult is { CallId: "agentic-plan-create-1" })
            {
                yield return CreatePlanStepUpdate(messageId: Guid.NewGuid().ToString("N"), stepIndex: 0);
                yield break;
            }

            if (functionResult?.CallId.StartsWith(
                "agentic-plan-step-",
                StringComparison.Ordinal) == true &&
                int.TryParse(functionResult.CallId["agentic-plan-step-".Length..], out var stepNumber) &&
                stepNumber >= 1 &&
                stepNumber < planSteps.Length)
            {
                yield return CreatePlanStepUpdate(
                    messageId: Guid.NewGuid().ToString("N"),
                    stepIndex: stepNumber);
                yield break;
            }

            var response = functionResult switch
            {
                { CallId: "backend-tool-weather-1" } =>
                    "The weather in San Francisco is sunny with a temperature of 20\u00b0C.",
                { CallId: "human-in-the-loop-steps-1" } result =>
                    CreateTaskStepsSummary(result.Result),
                { CallId: var callId }
                    when callId.StartsWith(
                        "tool-generative-ui-haiku-",
                        StringComparison.Ordinal) =>
                    "Your nature haiku is ready\u2014a quiet pond awakened by a frog.",
                { CallId: var callId }
                    when callId == $"agentic-plan-step-{planSteps.Length}" =>
                    $"All {planSteps.Length} steps in the " +
                    $"{(planSteps.Length == PizzaPlanSteps.Length ? "pizza" : "Mars mission")} plan are complete.",
                { CallId: "shared-state-recipe-1" }
                    when prompt.Contains("Italian", StringComparison.OrdinalIgnoreCase) =>
                    "The state now includes a detailed recipe for Classic Italian Carbonara, " +
                    "with specific ingredients, cooking instructions, and customization options " +
                    "for preferences like vegetarian alternatives. It also outlines the skill " +
                    "level, cooking time, and key steps for preparation.",
                { CallId: "shared-state-recipe-1" } =>
                    "I updated the shared recipe.",
                _ => "Background changed to a sunset gradient.",
            };
            if (functionResult is { CallId: "shared-state-recipe-1" })
            {
                var responseMessageId = Guid.NewGuid().ToString("N");
                var tokens = response.Split(' ');

                for (var i = 0; i < tokens.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(TokenDelay, cancellationToken);

                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        MessageId = responseMessageId,
                        Contents = [new TextContent(tokens[i] + " ")],
                        FinishReason = i == tokens.Length - 1 ? ChatFinishReason.Stop : null,
                    };
                }

                yield break;
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = Guid.NewGuid().ToString("N"),
                Contents = [new TextContent(response)],
                FinishReason = ChatFinishReason.Stop,
            };
            yield break;
        }

        var messageId = Guid.NewGuid().ToString("N");
        if (options?.Tools?.OfType<AIFunctionDeclaration>()
                .Any(tool => tool.Name == "generate_recipe") == true)
        {
            await Task.Delay(ModelDelay, cancellationToken);

            if (prompt.Contains("Italian", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    MessageId = messageId,
                    Contents =
                    [
                        new FunctionCallContent(
                            "shared-state-recipe-1",
                            "generate_recipe",
                            new Dictionary<string, object?>
                            {
                                ["recipe"] = CreateItalianCarbonara(),
                            })
                    ],
                    FinishReason = ChatFinishReason.ToolCalls,
                };
                yield break;
            }

            options.TryGetRunAgentInput(out var input);
            var current = input?.State?.Deserialize<RecipeResponse>(
                AIJsonUtilities.DefaultOptions)?.Recipe ?? new Recipe();
            var ingredients = current.Ingredients.ToList();
            if (!ingredients.Any(ingredient =>
                ingredient.Name.Equals("Fresh Basil", StringComparison.OrdinalIgnoreCase)))
            {
                ingredients.Add(new Ingredient
                {
                    Icon = "\U0001F33F",
                    Name = "Fresh Basil",
                    Amount = "1 handful",
                });
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        "shared-state-recipe-1",
                        "generate_recipe",
                        new Dictionary<string, object?>
                        {
                            ["recipe"] = new Recipe
                            {
                                Title = $"Italian {current.Title}",
                                SkillLevel = current.SkillLevel,
                                CookingTime = current.CookingTime,
                                SpecialPreferences = current.SpecialPreferences,
                                Ingredients = ingredients,
                                Instructions =
                                [
                                    .. current.Instructions,
                                    "Finish with fresh basil",
                                ],
                            },
                        })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        if (prompt.Contains("plan", StringComparison.OrdinalIgnoreCase) &&
            options?.Tools?.OfType<AIFunctionDeclaration>()
                .Any(tool => tool.Name == "create_plan") == true)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        "agentic-plan-create-1",
                        "create_plan",
                        new Dictionary<string, object?>
                        {
                            ["steps"] = planSteps,
                        })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        if (prompt.Contains("weather", StringComparison.OrdinalIgnoreCase) &&
            options?.Tools?.OfType<AIFunctionDeclaration>()
                .Any(tool => tool.Name == "get_weather") == true)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        "backend-tool-weather-1",
                        "get_weather",
                        new Dictionary<string, object?>
                        {
                            ["location"] = "San Francisco"
                        })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        if (prompt.Contains("background", StringComparison.OrdinalIgnoreCase) &&
            options?.Tools?.OfType<AIFunctionDeclaration>()
                .Any(tool => tool.Name == "change_background") == true)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        "agentic-chat-background-1",
                        "change_background",
                        new Dictionary<string, object?>
                        {
                            ["background"] = "linear-gradient(135deg, #ff9a9e, #fad0c4)"
                        })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        if (prompt.Contains("plan", StringComparison.OrdinalIgnoreCase) &&
            options?.Tools?.OfType<AIFunctionDeclaration>()
                .Any(tool => tool.Name == "generate_task_steps") == true)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        "human-in-the-loop-steps-1",
                        "generate_task_steps",
                        new Dictionary<string, object?>
                        {
                            ["steps"] = new[]
                            {
                                new { description = "Define mission goals and timeline", status = "enabled" },
                                new { description = "Design and test the spacecraft", status = "enabled" },
                                new { description = "Select and train the astronaut crew", status = "enabled" },
                                new { description = "Plan launch and Mars surface operations", status = "enabled" },
                                new { description = "Prepare communications and contingency plans", status = "enabled" },
                            },
                        })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        if (prompt.Contains("haiku", StringComparison.OrdinalIgnoreCase) &&
            options?.Tools?.OfType<AIFunctionDeclaration>()
                .Any(tool => tool.Name == "generate_haiku") == true)
        {
            var callId = $"tool-generative-ui-haiku-{Guid.NewGuid():N}";
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        callId,
                        "generate_haiku",
                        new Dictionary<string, object?>
                        {
                            ["japanese"] = new[] { "\u53e4\u6c60\u3084", "\u86d9\u98db\u3073\u3053\u3080", "\u6c34\u306e\u97f3" },
                            ["english"] = new[] { "An ancient pond\u2014", "A frog leaps in,", "The sound of water." },
                            ["image_name"] = "ancient-pond.svg",
                            ["gradient"] = "linear-gradient(135deg, #134e5e, #71b280)",
                        })
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        var answer = $"""
            ## Agentic response

            You said: **{prompt}**.

            - Streams over AG-UI SSE
            - Renders `structured` assistant content
            """;

        foreach (var token in answer.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TokenDelay, cancellationToken);

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents = [new TextContent(token + " ")],
            };
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

    private static Recipe CreateItalianCarbonara() => new()
    {
        Title = "Classic Italian Carbonara",
        SkillLevel = "Intermediate",
        CookingTime = "45 min",
        Ingredients =
        [
            new() { Icon = "\U0001F35D", Name = "Spaghetti", Amount = "400g" },
            new() { Icon = "\U0001F953", Name = "Guanciale (Pork Jowl)", Amount = "150g" },
            new() { Icon = "\U0001F95A", Name = "Egg Yolks", Amount = "4 yolks" },
            new() { Icon = "\U0001F9C0", Name = "Pecorino Romano Cheese", Amount = "100g, grated" },
            new() { Icon = "\U0001F9C2", Name = "Salt", Amount = "to taste" },
            new() { Icon = "\u26AB", Name = "Black Pepper", Amount = "Freshly ground, to taste" },
        ],
        Instructions =
        [
            "Start cooking your spaghetti in a large pot of lightly salted water until it's al dente.",
            "Meanwhile, dice the guanciale into small cubes and cook it in a skillet over medium heat until crispy.",
            "In a bowl, whisk together the egg yolks and grated cheese until smooth.",
            "Once the pasta is cooked, drain it, reserving half a cup of the cooking water.",
            "Combine the hot pasta with the guanciale, then remove the pan from the heat.",
            "Stir in the egg and cheese mixture, adding reserved pasta water until creamy.",
            "Season with freshly ground black pepper and serve immediately.",
        ],
    };

    private static ChatResponseUpdate CreatePlanStepUpdate(string messageId, int stepIndex)
    {
        return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = messageId,
            Contents =
            [
                new FunctionCallContent(
                    $"agentic-plan-step-{stepIndex + 1}",
                    "update_plan_step",
                    new Dictionary<string, object?>
                    {
                        ["index"] = stepIndex,
                        ["status"] = "completed",
                    })
            ],
            FinishReason = ChatFinishReason.ToolCalls,
        };
    }

    private static string CreateTaskStepsSummary(object? result)
    {
        var resultText = result switch
        {
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement element => element.ToString(),
            string text when text.StartsWith('"') =>
                JsonSerializer.Deserialize<string>(text),
            _ => result?.ToString(),
        };
        const string selectedPrefix = "The user selected the following steps:";
        if (resultText?.StartsWith(selectedPrefix, StringComparison.Ordinal) == true)
        {
            return $"I'll move forward with the selected tasks: {resultText[selectedPrefix.Length..].Trim()}.";
        }

        if (resultText?.Contains("rejected all proposed steps", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "No tasks were selected, so I won't move forward with any proposed steps.";
        }

        throw new InvalidOperationException(
            $"The generate_task_steps tool returned an unsupported result: '{resultText}'.");
    }
}
