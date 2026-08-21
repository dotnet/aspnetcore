// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Owns the state of a conversation: the turns rendered by the UI, the current
/// <see cref="ConversationStatus"/>, and the notifications the components subscribe to.
/// </summary>
/// <example>
/// <code>
/// var context = new AgentContext(agent);
/// using var subscription = context.RegisterOnStatusChanged(status => Console.WriteLine(status));
/// await context.SendMessageAsync("Hello");
/// </code>
/// </example>
public class AgentContext : IDisposable
{
    private readonly UIAgent _agent;
    private readonly List<ConversationTurn> _turns = new();
    private readonly List<Action<ConversationTurn>> _turnAddedCallbacks = new();
    private readonly List<Action<ConversationStatus>> _statusChangedCallbacks = new();
    private readonly List<Action<ConversationTurn, ContentBlock>> _blockAddedCallbacks = new();
    private CancellationTokenSource? _streamingCts;
    private ChatMessage? _lastMessage;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContext"/> class.
    /// </summary>
    /// <param name="agent">The agent that produces the responses for this conversation.</param>
    public AgentContext(UIAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agent = agent;
    }

    /// <summary>
    /// Gets the turns of this conversation, oldest first.
    /// </summary>
    public IReadOnlyList<ConversationTurn> Turns => _turns;

    /// <summary>
    /// Gets the current status of the conversation.
    /// </summary>
    public ConversationStatus Status { get; private set; }

    /// <summary>
    /// Gets the exception that failed the last turn, when <see cref="Status"/> is
    /// <see cref="ConversationStatus.Error"/>.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Sends a text message and streams the response into a new turn.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="cancellationToken">A token that cancels the response.</param>
    /// <returns>A task that completes when the turn finishes.</returns>
    public Task SendMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(new ChatMessage(ChatRole.User, text), cancellationToken);
    }

    /// <summary>
    /// Sends a message and streams the response into a new turn.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token that cancels the response.</param>
    /// <returns>A task that completes when the turn finishes.</returns>
    public async Task SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (Status is ConversationStatus.Streaming or ConversationStatus.AwaitingInput)
        {
            throw new InvalidOperationException("A message is already being processed.");
        }

        _lastMessage = message;

        var turn = new ConversationTurn();
        _turns.Add(turn);
        NotifyTurnAdded(turn);

        _streamingCts?.Dispose();
        _streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await StreamIntoTurnAsync(message, turn, _streamingCts.Token, cancellationToken);
    }

    /// <summary>
    /// Restores the committed turns from the agent's configured conversation thread.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels restoration.</param>
    /// <returns>A task that completes when the turns have been restored.</returns>
    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        if (Status == ConversationStatus.Streaming)
        {
            throw new InvalidOperationException("A message is already being processed.");
        }

        var blocks = await _agent.RestoreAsync(cancellationToken);
        _turns.Clear();

        ConversationTurn? currentTurn = null;
        foreach (var block in blocks)
        {
            if (block.Role == ChatRole.User)
            {
                currentTurn = new ConversationTurn();
                _turns.Add(currentTurn);
                currentTurn.AddRequestBlock(block);
            }
            else
            {
                if (currentTurn is null)
                {
                    currentTurn = new ConversationTurn();
                    _turns.Add(currentTurn);
                }

                currentTurn.AddResponseBlock(block);
            }
        }
    }

    /// <summary>
    /// Replays the last message after a failed turn.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the response.</param>
    /// <returns>A task that completes when the turn finishes.</returns>
    public async Task RetryAsync(CancellationToken cancellationToken = default)
    {
        if (Status != ConversationStatus.Error)
        {
            throw new InvalidOperationException(
                $"RetryAsync requires Status == Error, but Status is {Status}.");
        }

        var turn = _turns[^1];
        turn.ClearResponseBlocks();

        _streamingCts?.Dispose();
        _streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await StreamIntoTurnAsync(_lastMessage!, turn, _streamingCts.Token, cancellationToken);
    }

    /// <summary>
    /// Stops the response that is currently streaming, if any.
    /// </summary>
    /// <returns>A task that completes once cancellation has been requested.</returns>
    public Task CancelAsync()
    {
        if (Status is ConversationStatus.Idle or ConversationStatus.Error)
        {
            return Task.CompletedTask;
        }

        _streamingCts?.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers a callback invoked when a turn is added to the conversation.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <returns>A registration that removes the callback when disposed.</returns>
    public IDisposable RegisterOnTurnAdded(Action<ConversationTurn> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _turnAddedCallbacks.Add(callback);
        return new CallbackRegistration<Action<ConversationTurn>>(_turnAddedCallbacks, callback);
    }

    /// <summary>
    /// Registers a callback invoked when <see cref="Status"/> changes.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <returns>A registration that removes the callback when disposed.</returns>
    public IDisposable RegisterOnStatusChanged(Action<ConversationStatus> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _statusChangedCallbacks.Add(callback);
        return new CallbackRegistration<Action<ConversationStatus>>(_statusChangedCallbacks, callback);
    }

    /// <summary>
    /// Registers a callback invoked when a block is added to a turn.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <returns>A registration that removes the callback when disposed.</returns>
    public IDisposable RegisterOnBlockAdded(Action<ConversationTurn, ContentBlock> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _blockAddedCallbacks.Add(callback);
        return new CallbackRegistration<Action<ConversationTurn, ContentBlock>>(_blockAddedCallbacks, callback);
    }

    /// <summary>
    /// Releases the resources used by this context and stops any streaming response.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _streamingCts?.Cancel();
        _streamingCts?.Dispose();
        _turnAddedCallbacks.Clear();
        _statusChangedCallbacks.Clear();
        _blockAddedCallbacks.Clear();
        GC.SuppressFinalize(this);
    }

    private async Task StreamIntoTurnAsync(
        ChatMessage message,
        ConversationTurn turn,
        CancellationToken cancellationToken,
        CancellationToken callerToken)
    {
        Status = ConversationStatus.Streaming;
        Error = null;
        NotifyStatusChanged();

        try
        {
            IReadOnlyList<ChatMessage>? currentMessages = [message];
            while (currentMessages is not null)
            {
                var interactiveBlocks = new List<IInteractiveBlock>();

                await foreach (var block in _agent.SendMessagesAsync(currentMessages, cancellationToken)
                    .WithCancellation(cancellationToken))
                {
                    if (block.Role == message.Role)
                    {
                        turn.AddRequestBlock(block);
                    }
                    else
                    {
                        turn.AddResponseBlock(block);
                    }

                    if (block is IInteractiveBlock interactiveBlock)
                    {
                        interactiveBlocks.Add(interactiveBlock);
                    }

                    NotifyBlockAdded(turn, block);
                }

                if (interactiveBlocks.Count == 0)
                {
                    currentMessages = null;
                    continue;
                }

                Status = ConversationStatus.AwaitingInput;
                NotifyStatusChanged();

                var results = await Task.WhenAll(
                    interactiveBlocks.Select(block => block.GetResultAsync(cancellationToken)));

                currentMessages = CreateContinuationMessages(results);
                Status = ConversationStatus.Streaming;
                NotifyStatusChanged();
            }

            Status = ConversationStatus.Idle;
            if (cancellationToken.IsCancellationRequested)
            {
                turn.ClearResponseBlocks();
            }
            NotifyStatusChanged();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            turn.ClearResponseBlocks();
            Status = ConversationStatus.Idle;
            NotifyStatusChanged();
        }
        catch (Exception ex)
        {
            // A failing turn is surfaced as conversation state (Status/Error) rather than a
            // faulted Task: the UI renders the error and RetryAsync replays the last message.
            // This is the engine's error contract, not a swallowed exception.
            Error = ex;
            Status = ConversationStatus.Error;
            NotifyStatusChanged();
            return;
        }

        // If the caller's own token requested cancellation, surface it so the returned Task
        // completes as canceled. Cancellation driven by CancelAsync() (the internal token only)
        // is a graceful stop and completes normally.
        callerToken.ThrowIfCancellationRequested();
    }

    private static IReadOnlyList<ChatMessage> CreateContinuationMessages(
        IReadOnlyList<AIContent> results)
    {
        var messages = new List<ChatMessage>();
        foreach (var result in results)
        {
            var role = result is FunctionResultContent ? ChatRole.Tool : ChatRole.User;
            if (messages.Count > 0 && messages[^1].Role == role)
            {
                messages[^1].Contents.Add(result);
            }
            else
            {
                messages.Add(new ChatMessage(role, [result]));
            }
        }

        return messages;
    }

    private void NotifyStatusChanged()
    {
        var snapshot = _statusChangedCallbacks.ToArray();
        foreach (var callback in snapshot)
        {
            callback(Status);
        }
    }

    private void NotifyTurnAdded(ConversationTurn turn)
    {
        var snapshot = _turnAddedCallbacks.ToArray();
        foreach (var callback in snapshot)
        {
            callback(turn);
        }
    }

    private void NotifyBlockAdded(ConversationTurn turn, ContentBlock block)
    {
        var snapshot = _blockAddedCallbacks.ToArray();
        foreach (var callback in snapshot)
        {
            callback(turn, block);
        }
    }

    private sealed class CallbackRegistration<T> : IDisposable
    {
        private List<T>? _list;
        private T? _callback;

        internal CallbackRegistration(List<T> list, T callback)
        {
            _list = list;
            _callback = callback;
        }

        public void Dispose()
        {
            if (_list is not null && _callback is not null)
            {
                _list.Remove(_callback);
                _list = null;
                _callback = default;
            }
        }
    }
}
