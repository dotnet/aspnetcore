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

        var prompt = string.Empty;
        foreach (var message in messages)
        {
            if (message.Role == ChatRole.User)
            {
                prompt = message.Text;
            }
        }

        var messageId = Guid.NewGuid().ToString("N");
        var answer = $"You said: {prompt}. This is the scripted dojo agent answering without a live model.";

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
