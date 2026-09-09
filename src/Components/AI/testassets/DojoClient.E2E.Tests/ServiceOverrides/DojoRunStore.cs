// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using DojoAgent;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal sealed class DojoRunStore(IConfiguration configuration) : IAsyncDisposable
{
    internal const string RunKey = "dojo-test-run";
    internal const string ControlPath = "/_test/dojo-runs";

    private readonly ConcurrentDictionary<string, Run> _runs = new(StringComparer.Ordinal);

    internal async Task CreateAsync(string id, DojoRecording? recording)
    {
        var run = new Run(configuration, recording);
        if (!_runs.TryAdd(id, run))
        {
            await run.DisposeAsync();
            throw new InvalidOperationException($"Dojo test session '{id}' already exists.");
        }
    }

    internal Run Get(string id)
        => _runs.TryGetValue(id, out var run)
            ? run
            : throw new InvalidOperationException($"Dojo test session '{id}' does not exist.");

    internal async Task RemoveAsync(string id)
    {
        if (!_runs.TryRemove(id, out var run))
        {
            throw new InvalidOperationException($"Dojo test session '{id}' does not exist.");
        }

        await run.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var id in _runs.Keys)
        {
            if (_runs.TryRemove(id, out var run))
            {
                await run.DisposeAsync();
            }
        }
    }

    internal sealed class Run : IAsyncDisposable
    {
        private readonly object _lock = new();
        private readonly CancellationTokenSource _cancellation = new();
        private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IChatClient _client;
        private readonly IChatClient? _predictiveClient;
        private int _activeRequests;
        private bool _disposed;

        internal Run(IConfiguration configuration, DojoRecording? recording)
        {
            if (recording is { } selected)
            {
                var model = new RecordedChatClient(
                    RecordedScript.Load($"{selected}.recording.json"), Locks);
                _client = selected is DojoRecording.BackendToolRendering or
                    DojoRecording.AgenticGenerativeUI or DojoRecording.SharedState
                    ? new FunctionInvokingChatClient(model)
                    : model;
            }
            else
            {
                _client = ChatClientAgentFactory.CreateAgenticChat(configuration);
                _predictiveClient = ChatClientAgentFactory.CreatePredictiveStateUpdates(configuration);
            }
        }

        internal TestLockProvider Locks { get; } = new();

        internal async IAsyncEnumerable<ChatResponseUpdate> GetUpdatesAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            bool predictive,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            CancellationToken runToken;
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _activeRequests++;
                runToken = _cancellation.Token;
            }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(runToken, cancellationToken);
                var client = predictive ? _predictiveClient ?? _client : _client;
                await foreach (var update in client.GetStreamingResponseAsync(
                    messages, options, linked.Token).ConfigureAwait(false))
                {
                    yield return update;
                }
            }
            finally
            {
                lock (_lock)
                {
                    if (--_activeRequests == 0 && _disposed)
                    {
                        _drained.TrySetResult();
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                _disposed = true;
                if (_activeRequests == 0)
                {
                    _drained.TrySetResult();
                }
            }

            await _cancellation.CancelAsync();
            await _drained.Task;
            _client.Dispose();
            _predictiveClient?.Dispose();
            _cancellation.Dispose();
        }
    }
}
