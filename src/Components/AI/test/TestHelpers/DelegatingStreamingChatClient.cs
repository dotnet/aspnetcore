// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal sealed class DelegatingStreamingChatClient : IChatClient
{
    private Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken,
        IAsyncEnumerable<ChatResponseUpdate>>? _handler;

    internal void SetHandler(
        Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken,
            IAsyncEnumerable<ChatResponseUpdate>> handler)
    {
        _handler = handler;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_handler is null)
        {
            throw new InvalidOperationException(
                "No handler configured. Call SetHandler before GetStreamingResponseAsync.");
        }

        return _handler(messages, options, cancellationToken);
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "DelegatingStreamingChatClient only supports streaming. Use GetStreamingResponseAsync.");

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    public void Dispose() { }
}
