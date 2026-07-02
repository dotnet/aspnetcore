// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class UIAgent : IDisposable
{
    private readonly IChatClient _chatClient;
    private readonly UIAgentOptions _options;
    private readonly List<ChatMessage> _history = new();
    private bool _disposed;

    internal UIAgentOptions Options => _options;

    public UIAgent(IChatClient chatClient)
        : this(chatClient, configure: null)
    {
    }

    public UIAgent(IChatClient chatClient, ChatOptions chatOptions)
        : this(chatClient, options => options.ChatOptions = chatOptions)
    {
    }

    public UIAgent(IChatClient chatClient, Action<UIAgentOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
        _options = new UIAgentOptions();
        configure?.Invoke(_options);
    }

    public async IAsyncEnumerable<ContentBlock> SendMessageAsync(
        ChatMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _history.Add(message);

        var pipeline = new BlockMappingPipeline(_options);

        // Process user message through pipeline
        var userUpdate = new ChatResponseUpdate
        {
            Role = message.Role,
            Contents = [.. message.Contents]
        };
        await foreach (var block in pipeline.Process(userUpdate, cancellationToken).ConfigureAwait(false))
        {
            yield return block;
        }
        foreach (var block in pipeline.Finalize())
        {
            yield return block;
        }

        // Stream assistant response
        var assistantUpdates = new List<ChatResponseUpdate>();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(
            _history, _options.ChatOptions, cancellationToken).ConfigureAwait(false))
        {
            assistantUpdates.Add(update);

            await foreach (var block in pipeline.Process(update, cancellationToken).ConfigureAwait(false))
            {
                yield return block;
            }
        }

        foreach (var block in pipeline.Finalize())
        {
            yield return block;
        }

        // Add assistant response to history
        var response = assistantUpdates.ToChatResponse();
        foreach (var msg in response.Messages)
        {
            _history.Add(msg);
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
