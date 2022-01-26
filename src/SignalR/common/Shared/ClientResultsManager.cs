// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal class ClientResultsManager : IInvocationBinder
{
    private readonly ConcurrentDictionary<string, (Type Type, string ConnectionId, Func<CompletionMessage, Task> Completion)> _pendingInvocations = new();

    public Task<T> AddInvocation<T>(string connectionId, string invocationId, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSourceWithCancellation<T>(this, connectionId, invocationId, cancellationToken);
        _pendingInvocations.TryAdd(invocationId, (typeof(T), connectionId, completionMessage =>
        {
            if (completionMessage.HasResult)
            {
                tcs.SetResult((T)completionMessage.Result);
            }
            else
            {
                tcs.SetException(new Exception(completionMessage.Error));
            }
            return Task.CompletedTask;
        }
        ));

        return tcs.Task;
    }

    public void AddInvocation(string invocationId, (Type Type, string ConnectionId, Func<CompletionMessage, Task> Completion) invocationInfo)
    {
        _pendingInvocations.TryAdd(invocationId, invocationInfo);
    }

    public Task TryCompleteResult(string connectionId, CompletionMessage message)
    {
        if (_pendingInvocations.TryGetValue(message.InvocationId!, out var item))
        {
            if (item.ConnectionId != connectionId)
            {
                throw new Exception("wrong ID");
            }

            // if false the connection disconnected right after the above TryGetValue
            // or someone else completed the invocation (likely a bad client)
            // we'll ignore both cases
            if (_pendingInvocations.Remove(message.InvocationId!, out _))
            {
                return item.Completion(message);
            }
        }
        else
        {
            // connection was disconnected or someone else completed the invocation
        }
        return Task.CompletedTask;
    }

    public (Type Type, string ConnectionId, Func<CompletionMessage, Task> Completion) RemoveInvocation(string invocationId)
    {
        _pendingInvocations.Remove(invocationId, out var item);
        return item;
    }

    public void CleanupConnection(string connectionId, List<Task>? pendingTasks)
    {
        var invocationIds = _pendingInvocations.Where(x => x.Value.ConnectionId == connectionId).Select(x => x.Key);
        foreach (var id in invocationIds)
        {
            if (_pendingInvocations.Remove(id, out var item))
            {
                var task = item.Completion(CompletionMessage.WithError(id, "Connection disconnected"));
                if (!task.IsCompletedSuccessfully)
                {
                    if (pendingTasks is null)
                    {
                        pendingTasks = new List<Task>();
                    }
                    pendingTasks.Add(task);
                }
            }
        }
    }

    public bool TryGetType(string invocationId, [NotNullWhen(true)] out Type? type)
    {
        if (_pendingInvocations.TryGetValue(invocationId, out var item))
        {
            type = item.Type;
            return true;
        }
        type = null;
        return false;
    }

    public Type GetReturnType(string invocationId)
    {
        if (TryGetType(invocationId, out var type))
        {
            return type;
        }
        throw new InvalidOperationException();
    }

    public IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        throw new NotImplementedException();
    }

    public Type GetStreamItemType(string streamId)
    {
        throw new NotImplementedException();
    }

    private sealed class TaskCompletionSourceWithCancellation<T> : TaskCompletionSource<T>
    {
        private readonly ClientResultsManager _clientResultsManager;
        private readonly string _connectionId;
        private readonly string _invocationId;
        private readonly CancellationTokenRegistration _tokenRegistration;

        public TaskCompletionSourceWithCancellation(ClientResultsManager clientResultsManager, string connectionId, string invocationId,
            CancellationToken cancellationToken)
            : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            _clientResultsManager = clientResultsManager;
            _connectionId = connectionId;
            _invocationId = invocationId;

            if (cancellationToken.CanBeCanceled)
            {
                _tokenRegistration = cancellationToken.UnsafeRegister(static o =>
                {
                    var tcs = (TaskCompletionSourceWithCancellation<T>)o!;
                    tcs.SetCanceled();
                }, this);
            }
        }

        public new void SetCanceled()
        {
            // TODO: RedisHubLifetimeManager will want to notify the other server (if there is one) about the cancellation
            // so it can clean up state and potentially forward that info to the connection
            _ = _clientResultsManager.TryCompleteResult(_connectionId, CompletionMessage.WithError(_invocationId, "Canceled"));
        }

        public new void SetResult(T result)
        {
            base.SetResult(result);
            _tokenRegistration.Dispose();
        }

        public new void SetException(Exception exception)
        {
            base.SetException(exception);
            _tokenRegistration.Dispose();
        }

#pragma warning disable IDE0060 // Remove unused parameter
        // Just making sure we don't accidentally call one of these without knowing
        public static new void SetCanceled(CancellationToken cancellationToken) => Debug.Assert(false);
        public static new void SetException(IEnumerable<Exception> exceptions) => Debug.Assert(false);
        public static new bool TrySetCanceled()
        {
            Debug.Assert(false);
            return false;
        }
        public static new bool TrySetCanceled(CancellationToken cancellationToken)
        {
            Debug.Assert(false);
            return false;
        }
        public static new bool TrySetException(IEnumerable<Exception> exceptions)
        {
            Debug.Assert(false);
            return false;
        }
        public static new bool TrySetException(Exception exception)
        {
            Debug.Assert(false);
            return false;
        }
        public static new bool TrySetResult(T result)
        {
            Debug.Assert(false);
            return false;
        }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
