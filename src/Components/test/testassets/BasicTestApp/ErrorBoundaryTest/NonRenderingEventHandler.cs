// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BasicTestApp.ErrorBoundaryTest;

internal static class NonRenderingEventHandler
{
    public static Action Wrap(Action callback) => new SyncReceiver(callback).Invoke;

    public static Func<Task> Wrap(Func<Task> callback) => new AsyncReceiver(callback).Invoke;

    private sealed class SyncReceiver(Action callback) : IHandleEvent
    {
        public void Invoke() => callback();

        public Task HandleEventAsync(EventCallbackWorkItem item, object arg) => item.InvokeAsync(arg);
    }

    private sealed class AsyncReceiver(Func<Task> callback) : IHandleEvent
    {
        public Task Invoke() => callback();

        public Task HandleEventAsync(EventCallbackWorkItem item, object arg) => item.InvokeAsync(arg);
    }
}
