// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public abstract class ContentBlock
{
    public string Id { get; internal set; } = string.Empty;

    public BlockLifecycleState LifecycleState { get; internal set; }

    public ChatRole? Role { get; internal set; }

    public string? AuthorName { get; internal set; }

    private readonly List<Action> _callbacks = new();

    public ContentBlockChangedSubscription OnChanged(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callbacks.Add(callback);
        return new ContentBlockChangedSubscription(this, callback);
    }

    protected void NotifyChanged()
    {
        // Snapshot the callbacks to allow safe removal during iteration
        var snapshot = _callbacks.ToArray();
        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i]();
        }
    }

    internal void InvokeNotifyChanged() => NotifyChanged();

    internal void RemoveCallback(Action callback)
    {
        _callbacks.Remove(callback);
    }
}
