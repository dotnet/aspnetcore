// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public abstract class ContentBlock
{
    public string Id { get; set; } = string.Empty;

    public BlockLifecycleState LifecycleState { get; set; }

    public ChatRole? Role { get; set; }

    public string? AuthorName { get; set; }

    private readonly List<Action> _callbacks = new();

    public ContentBlockChangedSubscription OnChanged(Action callback)
    {
        _callbacks.Add(callback);
        return new ContentBlockChangedSubscription(this, callback);
    }

    protected void NotifyChanged()
    {
        for (var i = 0; i < _callbacks.Count; i++)
        {
            _callbacks[i]();
        }
    }

    internal void InvokeNotifyChanged() => NotifyChanged();

    internal void RemoveCallback(Action callback)
    {
        _callbacks.Remove(callback);
    }
}
