// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

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

    public AgentContext(UIAgent agent)
    {
        _agent = agent;
    }

    public IReadOnlyList<ConversationTurn> Turns => _turns;

    public ConversationStatus Status { get; private set; }

    public Exception? Error { get; private set; }

    public Task SendMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(new ChatMessage(ChatRole.User, text), cancellationToken);
    }

    public async Task SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        if (Status == ConversationStatus.Streaming)
        {
            throw new InvalidOperationException("A message is already being processed.");
        }

        _lastMessage = message;

        var turn = new ConversationTurn();
        _turns.Add(turn);
        NotifyTurnAdded(turn);

        _streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await StreamIntoTurnAsync(message, turn, _streamingCts.Token);
    }

    private async Task StreamIntoTurnAsync(
        ChatMessage message,
        ConversationTurn turn,
        CancellationToken cancellationToken)
    {
        Status = ConversationStatus.Streaming;
        Error = null;
        NotifyStatusChanged();

        try
        {
            ChatMessage? currentMessage = message;

            while (currentMessage is not null)
            {
                var interactiveBlocks = new List<IInteractiveBlock>();
                var uninvokedToolBlocks = new List<FunctionInvocationContentBlock>();

                await foreach (var block in _agent.SendMessageAsync(currentMessage, cancellationToken)
                    .WithCancellation(cancellationToken))
                {
                    if (block is IInteractiveBlock interactive)
                    {
                        interactiveBlocks.Add(interactive);
                    }
                    else if (block is FunctionInvocationContentBlock ficb
                             && ficb.Call is { InformationalOnly: false }
                             && ficb.Result is null)
                    {
                        uninvokedToolBlocks.Add(ficb);
                    }

                    if (block.Role == currentMessage.Role)
                    {
                        turn.AddRequestBlock(block);
                    }
                    else
                    {
                        turn.AddResponseBlock(block);
                    }

                    NotifyBlockAdded(turn, block);
                }

                currentMessage = null;

                if (interactiveBlocks.Count == 0 && uninvokedToolBlocks.Count == 0)
                {
                    break;
                }

                // Build tasks for all pending work
                var resultTasks = new List<Task<AIContent>>();

                foreach (var interactive in interactiveBlocks)
                {
                    resultTasks.Add(interactive.GetResultAsync(cancellationToken));
                }

                foreach (var toolBlock in uninvokedToolBlocks)
                {
                    resultTasks.Add(InvokeBackendToolAsync(toolBlock, cancellationToken));
                }

                if (interactiveBlocks.Count > 0)
                {
                    Status = ConversationStatus.AwaitingInput;
                    NotifyStatusChanged();
                }

                var results = await Task.WhenAll(resultTasks);

                if (results.Length > 0)
                {
                    var role = results.Any(r => r is not FunctionResultContent)
                        ? ChatRole.User
                        : ChatRole.Tool;
                    currentMessage = new ChatMessage(role, [.. results]);
                }

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
            Error = ex;
            Status = ConversationStatus.Error;
            NotifyStatusChanged();
        }
    }

    private async Task<AIContent> InvokeBackendToolAsync(
        FunctionInvocationContentBlock block,
        CancellationToken cancellationToken)
    {
        var result = await _agent.InvokeToolAsync(block.Call!, cancellationToken);
        block.Result = result;
        block.InvokeNotifyChanged();
        return result;
    }

    public async Task RetryAsync(CancellationToken cancellationToken = default)
    {
        if (Status != ConversationStatus.Error)
        {
            throw new InvalidOperationException(
                $"RetryAsync requires Status == Error, but Status is {Status}.");
        }

        var turn = _turns[^1];
        turn.ClearResponseBlocks();

        _streamingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await StreamIntoTurnAsync(_lastMessage!, turn, _streamingCts.Token);
    }

    public Task CancelAsync()
    {
        if (Status == ConversationStatus.Idle || Status == ConversationStatus.Error)
        {
            return Task.CompletedTask;
        }

        _streamingCts?.Cancel();
        return Task.CompletedTask;
    }

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
    }

    public IDisposable RegisterOnTurnAdded(Action<ConversationTurn> callback)
    {
        _turnAddedCallbacks.Add(callback);
        return new CallbackRegistration<Action<ConversationTurn>>(_turnAddedCallbacks, callback);
    }

    public IDisposable RegisterOnStatusChanged(Action<ConversationStatus> callback)
    {
        _statusChangedCallbacks.Add(callback);
        return new CallbackRegistration<Action<ConversationStatus>>(_statusChangedCallbacks, callback);
    }

    public IDisposable RegisterOnBlockAdded(Action<ConversationTurn, ContentBlock> callback)
    {
        _blockAddedCallbacks.Add(callback);
        return new CallbackRegistration<Action<ConversationTurn, ContentBlock>>(_blockAddedCallbacks, callback);
    }

    private void NotifyStatusChanged()
    {
        var snapshot = _statusChangedCallbacks.ToArray();
        foreach (var cb in snapshot)
        {
            cb(Status);
        }
    }

    private void NotifyTurnAdded(ConversationTurn turn)
    {
        var snapshot = _turnAddedCallbacks.ToArray();
        foreach (var cb in snapshot)
        {
            cb(turn);
        }
    }

    private void NotifyBlockAdded(ConversationTurn turn, ContentBlock block)
    {
        var snapshot = _blockAddedCallbacks.ToArray();
        foreach (var cb in snapshot)
        {
            cb(turn, block);
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
