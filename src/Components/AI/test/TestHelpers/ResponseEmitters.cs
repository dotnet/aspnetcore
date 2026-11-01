// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal static class ResponseEmitters
{
    internal static async IAsyncEnumerable<ChatResponseUpdate> EmitTextResponse(
        string text,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents = [new TextContent(text)]
        };
        await Task.CompletedTask;
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> EmitMultiTokenTextResponse(
        [EnumeratorCancellation] CancellationToken ct = default,
        params string[] tokens)
    {
        var messageId = Guid.NewGuid().ToString("N");
        foreach (var token in tokens)
        {
            ct.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents = [new TextContent(token)]
            };
        }
        await Task.CompletedTask;
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> EmitEmptyResponse(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> EmitErrorAfterTokens(
        string[] tokens,
        Exception error,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messageId = Guid.NewGuid().ToString("N");
        foreach (var token in tokens)
        {
            ct.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents = [new TextContent(token)]
            };
        }

        await Task.CompletedTask;
        throw error;
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> EmitTokensWithGate(
        string[] tokens,
        Func<int, Task> gate,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messageId = Guid.NewGuid().ToString("N");
        for (var i = 0; i < tokens.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            await gate(i);
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = messageId,
                Contents = [new TextContent(tokens[i])]
            };
        }
    }
}
