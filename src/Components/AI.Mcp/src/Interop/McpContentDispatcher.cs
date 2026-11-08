// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal sealed class McpContentDispatcher : IDisposable
{
    private readonly object _lock = new();
    private readonly Dictionary<RequestId, CancellationTokenSource> _pending = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcMessage>> _handler;
    private bool _closed;

    public McpContentDispatcher(Func<JsonRpcRequest, CancellationToken, ValueTask<JsonRpcMessage>> handler)
    {
        _handler = handler;
    }

    public async Task<string> DispatchAsync(string json)
    {
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, McpJsonUtilities.DefaultOptions)
            ?? throw new JsonException("The MCP app request cannot be null.");
        if (request.JsonRpc != "2.0")
        {
            throw new JsonException("The MCP app request must use JSON-RPC 2.0.");
        }
        var id = request.Id;
        CancellationTokenSource operation;
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            if (_pending.ContainsKey(id))
            {
                throw new InvalidOperationException("The request identity is already pending.");
            }
            operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _pending.Add(id, operation);
        }

        Task<JsonRpcMessage>? handlerTask = null;
        try
        {
            handlerTask = _handler(request, operation.Token).AsTask();
            var response = await handlerTask.WaitAsync(operation.Token);
            var result = JsonSerializer.Serialize(response, McpJsonUtilities.DefaultOptions);
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

    private void Release(RequestId id, CancellationTokenSource operation)
    {
        lock (_lock)
        {
            _pending.Remove(id);
        }
        operation.Dispose();
    }

    public void Cancel(RequestId id)
    {
        lock (_lock)
        {
            if (_pending.TryGetValue(id, out var operation))
            {
                operation.Cancel();
            }
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
