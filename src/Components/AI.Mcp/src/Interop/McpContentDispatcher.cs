// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal sealed class McpContentDispatcher : IDisposable
{
    private readonly object _lock = new();
    private readonly Dictionary<string, CancellationTokenSource> _pending = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly McpContentOptions _options;
    private readonly Func<McpAppRequest, CancellationToken, ValueTask<McpAppResponse>> _handler;
    private bool _closed;

    public McpContentDispatcher(McpContentOptions options,
        Func<McpAppRequest, CancellationToken, ValueTask<McpAppResponse>> handler)
    {
        options.Validate();
        _options = options;
        _handler = handler;
    }

    public async Task<string> DispatchAsync(string id, string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        CheckSize(json);
        CancellationTokenSource operation;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            if (_pending.ContainsKey(id))
            {
                throw new InvalidOperationException("The request identity is already pending.");
            }
            if (_pending.Count >= _options.MaximumPendingRequests)
            {
                throw new InvalidOperationException("The MCP app request limit was reached.");
            }
            operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            operation.CancelAfter(_options.OperationTimeout);
            _pending.Add(id, operation);
        }

        Task<McpAppResponse>? handlerTask = null;
        try
        {
            var request = JsonSerializer.Deserialize(json, McpAppJsonContext.Default.McpAppRequest)
                ?? throw new JsonException("The MCP app request cannot be null.");
            handlerTask = _handler(request, operation.Token).AsTask();
            var response = await handlerTask.WaitAsync(operation.Token);
            var result = JsonSerializer.Serialize(response, McpAppJsonContext.Default.McpAppResponse);
            CheckSize(result);
            return result;
        }
        finally
        {
            if (handlerTask is null || handlerTask.IsCompleted)
            {
                Release(id, operation);
            }
            else
            {
                _ = handlerTask.ContinueWith(task =>
                {
                    _ = task.Exception;
                    Release(id, operation);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
    }

    private void Release(string id, CancellationTokenSource operation)
    {
        lock (_lock)
        {
            _pending.Remove(id);
        }
        operation.Dispose();
    }

    public void Cancel(string id)
    {
        lock (_lock)
        {
            if (_pending.TryGetValue(id, out var operation))
            {
                operation.Cancel();
            }
        }
    }

    private void CheckSize(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > _options.MaximumMessageBytes)
        {
            throw new InvalidOperationException("The MCP app message exceeds the configured byte limit.");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_closed)
            {
                return;
            }
            _closed = true;
            _lifetime.Cancel();
        }
    }
}
