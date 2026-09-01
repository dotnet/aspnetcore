// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Turns an <see cref="IChatClient"/> stream into content blocks the UI can render as they
/// arrive, and keeps the conversation history for subsequent turns.
/// </summary>
/// <remarks>
/// A <see cref="UIAgent"/> is protocol- and provider-neutral: it only depends on
/// <see cref="IChatClient"/>, so any Microsoft.Extensions.AI client can drive it.
/// </remarks>
/// <example>
/// <code>
/// var agent = new UIAgent(chatClient);
/// await foreach (var block in agent.SendMessageAsync(new ChatMessage(ChatRole.User, "Hello")))
/// {
///     Console.WriteLine(block.Id);
/// }
/// </code>
/// </example>
public class UIAgent : IDisposable
{
    private readonly IChatClient _chatClient;
    private readonly UIAgentOptions _options;
    private readonly ILogger _logger;
    private readonly List<ChatMessage> _history = new();
    private bool _disposed;

    internal UIAgentOptions Options => _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    public UIAgent(IChatClient chatClient)
        : this(chatClient, configure: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="chatOptions">The options passed to the chat client.</param>
    public UIAgent(IChatClient chatClient, ChatOptions chatOptions)
        : this(chatClient, options => options.ChatOptions = chatOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="chatOptions">The options passed to the chat client.</param>
    /// <param name="loggerFactory">The logger factory used to trace block mapping.</param>
    public UIAgent(IChatClient chatClient, ChatOptions chatOptions, ILoggerFactory? loggerFactory)
        : this(chatClient, options => options.ChatOptions = chatOptions, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="configure">A callback that configures the agent.</param>
    public UIAgent(IChatClient chatClient, Action<UIAgentOptions>? configure)
        : this(chatClient, configure, loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAgent"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client that produces model responses.</param>
    /// <param name="configure">A callback that configures the agent.</param>
    /// <param name="loggerFactory">The logger factory used to trace block mapping.</param>
    public UIAgent(IChatClient chatClient, Action<UIAgentOptions>? configure, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
        _options = new UIAgentOptions();
        configure?.Invoke(_options);
        _logger = (ILogger?)loggerFactory?.CreateLogger<BlockMappingPipeline>() ?? NullLogger.Instance;
    }

    internal UIAgent(IChatClient chatClient, UIAgentOptions options, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
        _options = options;
        _logger = (ILogger?)loggerFactory?.CreateLogger<BlockMappingPipeline>() ?? NullLogger.Instance;
    }

    /// <summary>
    /// Sends a message and streams the resulting content blocks. Blocks are yielded as soon as
    /// they are created; a block keeps changing (raising <see cref="ContentBlock.OnChanged(Action)"/>)
    /// until it becomes <see cref="BlockLifecycleState.Inactive"/>.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token that cancels the response.</param>
    /// <returns>The blocks produced by the message and by the model response to it.</returns>
    public async IAsyncEnumerable<ContentBlock> SendMessageAsync(
        ChatMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await foreach (var block in SendMessagesAsync([message], cancellationToken).ConfigureAwait(false))
        {
            yield return block;
        }
    }

    internal async IAsyncEnumerable<ContentBlock> SendMessagesAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var thread = _options.Thread;
        foreach (var message in messages)
        {
            ArgumentNullException.ThrowIfNull(message);
            _history.Add(message);
        }

        var pipeline = new BlockMappingPipeline(_options, _logger);

        // Process user messages through pipeline
        foreach (var message in messages)
        {
            var userUpdate = new ChatResponseUpdate
            {
                Role = message.Role,
                Contents = [.. message.Contents]
            };
            await foreach (var block in pipeline.Process(userUpdate, cancellationToken).ConfigureAwait(false))
            {
                yield return block;
            }
        }

        foreach (var block in pipeline.Finalize())
        {
            yield return block;
        }

        // Stream assistant response
        UIAgentLog.StreamingAssistantResponse(_logger);
        var responseUpdates = new List<ChatResponseUpdate>();
        var assistantUpdates = new List<ChatResponseUpdate>();
        var updateIndex = 0;
        var chatOptions = BuildChatOptions();
        IEnumerable<ChatMessage> requestMessages = _history;
        if (thread is { IsStateful: true, ConversationId: not null })
        {
            chatOptions = chatOptions?.Clone() ?? new ChatOptions();
            chatOptions.ConversationId = thread.ConversationId;
            requestMessages = messages;
        }

        await foreach (var update in _chatClient.GetStreamingResponseAsync(
            requestMessages, chatOptions, cancellationToken).ConfigureAwait(false))
        {
            var contentTypes = string.Join(", ", update.Contents.Select(c => c.GetType().Name));
            UIAgentLog.ReceivedUpdate(_logger, updateIndex++, update.Role?.Value, contentTypes);

            responseUpdates.Add(update);
            var processUpdate = ApplyStateMapper(update);
            assistantUpdates.Add(processUpdate);

            if (processUpdate.Contents.Count == 0 && update.Contents.Count > 0)
            {
                continue;
            }

            await foreach (var block in pipeline.Process(processUpdate, cancellationToken).ConfigureAwait(false))
            {
                yield return block;
            }
        }

        UIAgentLog.StreamComplete(_logger, assistantUpdates.Count);

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

        if (thread is not null && messages.Count > 0)
        {
            thread.AppendUserMessage(messages[0]);
            foreach (var message in messages.Skip(1))
            {
                thread.AppendUpdate(new ChatResponseUpdate
                {
                    Role = message.Role,
                    Contents = [.. message.Contents],
                });
            }

            foreach (var update in responseUpdates)
            {
                thread.AppendUpdate(update);
            }

            thread.CompleteTurn();
        }

        UIAgentLog.AddedToHistory(_logger, response.Messages.Count);
    }

    /// <summary>
    /// Restores the committed conversation and typed state from the configured thread.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels restoration.</param>
    /// <returns>The restored content blocks in chronological order.</returns>
    public async Task<IReadOnlyList<ContentBlock>> RestoreAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var updates = _options.Thread?.GetUpdates();
        if (updates is not { Count: > 0 })
        {
            return [];
        }

        _history.Clear();

        var blocks = new List<ContentBlock>();
        var pipeline = new BlockMappingPipeline(_options, _logger);
        var assistantUpdates = new List<ChatResponseUpdate>();

        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (update.Role == ChatRole.User)
            {
                if (assistantUpdates.Count > 0)
                {
                    AddResponseToHistory(assistantUpdates);
                    assistantUpdates.Clear();

                    blocks.AddRange(pipeline.Finalize());
                    pipeline = new BlockMappingPipeline(_options, _logger);
                }

                _history.Add(new ChatMessage(update.Role.Value, [.. update.Contents]));
                await foreach (var block in pipeline.Process(update, cancellationToken).ConfigureAwait(false))
                {
                    blocks.Add(block);
                }

                blocks.AddRange(pipeline.Finalize());
                pipeline = new BlockMappingPipeline(_options, _logger);
            }
            else
            {
                assistantUpdates.Add(update);

                var processUpdate = ApplyStateMapper(update);
                if (processUpdate.Contents.Count == 0 && update.Contents.Count > 0)
                {
                    continue;
                }

                await foreach (var block in pipeline.Process(processUpdate, cancellationToken).ConfigureAwait(false))
                {
                    blocks.Add(block);
                }
            }
        }

        if (assistantUpdates.Count > 0)
        {
            AddResponseToHistory(assistantUpdates);
        }

        blocks.AddRange(pipeline.Finalize());

        return blocks;
    }

    internal virtual ChatResponseUpdate ApplyStateMapper(ChatResponseUpdate update)
    {
        if (_options.StateMapper is null)
        {
            return update;
        }

        var context = new StateMapperContext(update);
        _options.StateMapper(context);

        return context.HasHandledContent ? context.GetFilteredUpdate() : update;
    }

    private void AddResponseToHistory(List<ChatResponseUpdate> updates)
    {
        var response = updates.ToChatResponse();
        foreach (var message in response.Messages)
        {
            _history.Add(message);
        }
    }

    private ChatOptions? BuildChatOptions()
    {
        if (_options.UIActions.Count == 0)
        {
            return _options.ChatOptions;
        }

        var chatOptions = _options.ChatOptions?.Clone() ?? new ChatOptions();
        var tools = chatOptions.Tools is null
            ? new List<AITool>()
            : [.. chatOptions.Tools];

        foreach (var action in _options.UIActions.Values)
        {
            tools.Add(action.AsDeclarationOnly());
        }

        chatOptions.Tools = tools;
        return chatOptions;
    }

    /// <summary>
    /// Releases the resources used by this agent.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
