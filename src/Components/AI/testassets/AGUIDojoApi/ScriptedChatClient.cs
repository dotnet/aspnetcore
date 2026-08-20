// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = Guid.NewGuid().ToString("N"),
                Contents = [new TextContent("Background changed to a sunset gradient.")],
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
}
