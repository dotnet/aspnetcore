// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Components.TestServer.Services;

public class AsyncOperationService
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _tasks = new();

    public Task Start(string id)
    {
        return _tasks.GetOrAdd(id, (id) => new TaskCompletionSource()).Task;
    }

    public void Complete(string id)
    {
        if (_tasks.TryRemove(id, out var tcs))
        {
            tcs.SetResult();
        }
    }
}
