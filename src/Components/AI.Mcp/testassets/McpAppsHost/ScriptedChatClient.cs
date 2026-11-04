// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace McpAppsHost;

public sealed class ScriptedChatClient(HostState state) : IChatClient
{
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref state.ModelRequests);
        state.ModelToolNames = options?.Tools?.Select(tool => tool.Name).ToArray() ?? [];
        var history = messages.ToList();
        var results = history.SelectMany(message => message.Contents).OfType<FunctionResultContent>().ToArray();
        foreach (var result in results)
        {
            var value = result.Result switch
            {
                JsonElement element => element.GetRawText(),
                null => "",
                _ => JsonSerializer.Serialize(result.Result),
            };
            if (value.Contains("Not for the model.", StringComparison.Ordinal))
            {
                state.ReceivedViewMetadata = true;
            }
        }

        if (results.Length == 0)
        {
            var tool = options?.Tools?.Single(tool => tool.Name == "show_note")
                ?? throw new InvalidOperationException("The model-visible note tool is missing.");
            var calls = new List<AIContent>
            {
                new FunctionCallContent("original-note-call", tool.Name, new Dictionary<string, object?> { ["title"] = "Original note" })
            };
            if (history.Last(message => message.Role == ChatRole.User).Text == "Open two notes")
            {
                calls.Add(new FunctionCallContent("second-note-call", tool.Name,
                    new Dictionary<string, object?> { ["title"] = "Second note" }));
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "note-call",
                Contents = calls,
                FinishReason = ChatFinishReason.ToolCalls,
            };
        }
        else
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "note-response",
                Contents = [new TextContent("The original note was opened.")],
            };

            if (state.HoldResponse)
            {
                await state.Release.Task.WaitAsync(cancellationToken);
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = "note-response",
                FinishReason = ChatFinishReason.Stop,
            };
        }
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(IChatClient) ? this : null;
    }

    public void Dispose()
    {
    }
}
