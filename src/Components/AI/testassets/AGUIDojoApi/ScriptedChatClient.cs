// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AGUIDojoApi;

// Local stand-in for a model, used when no live model is configured. It streams a canned
// answer one word at a time so the dojo can be exercised end to end (including incremental
// rendering) without any credentials.
internal sealed class ScriptedChatClient : IChatClient
{
    private static readonly TimeSpan TokenDelay = TimeSpan.FromMilliseconds(60);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        if (messageList.LastOrDefault()?.Role == ChatRole.Tool)
        {
            var functionResult = messageList[^1].Contents
                .OfType<FunctionResultContent>()
                .SingleOrDefault();
            var response = functionResult switch
            {
                { CallId: "backend-tool-weather-1" } =>
                    "The weather in San Francisco is sunny with a temperature of 20\u00b0C.",
                { CallId: "human-in-the-loop-steps-1" } result =>
                    CreateTaskStepsSummary(result.Result),
                { CallId: "tool-generative-ui-haiku-1" } =>
                    "Your nature haiku is ready\u2014a quiet pond awakened by a frog.",
                _ => "Background changed to a sunset gradient.",
            };
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = Guid.NewGuid().ToString("N"),
                Contents = [new TextContent(response)],
                FinishReason = ChatFinishReason.Stop,
            };
            yield break;
        }

        var prompt = string.Empty;
        foreach (var message in messageList)
        {
            if (message.Role == ChatRole.User)
            {
                prompt = message.Text;
            }
        }

        var messageId = Guid.NewGuid().ToString("N");
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
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents =
                [
                    new FunctionCallContent(
                        "tool-generative-ui-haiku-1",
                        "generate_haiku",
                        new Dictionary<string, object?>
                        {
                            ["japanese"] = new[] { "\u53e4\u6c60\u3084", "\u86d9\u98db\u3073\u3053\u3080", "\u6c34\u306e\u97f3" },
                            ["english"] = new[] { "An ancient pond\u2014", "A frog leaps in,", "The sound of water." },
                            ["image_name"] = "ancient-pond",
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
