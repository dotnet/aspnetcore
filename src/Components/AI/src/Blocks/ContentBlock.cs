// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Base class for a unit of conversation content that the UI renders and that can be
/// updated while the model response is still streaming.
/// </summary>
/// <example>
/// Subscribe to updates while a block streams:
/// <code>
/// using var subscription = block.OnChanged(() => StateHasChanged());
/// </code>
/// </example>
public abstract class ContentBlock
{
    private readonly List<Action> _callbacks = new();

    /// <summary>
    /// Gets the identifier of this block. Blocks produced by the same model message share the same identifier.
    /// </summary>
    public string Id { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the current lifecycle state of this block.
    /// </summary>
    public BlockLifecycleState LifecycleState { get; internal set; }

    /// <summary>
    /// Gets the role of the conversation participant that produced this block.
    /// </summary>
    public ChatRole? Role { get; internal set; }

    /// <summary>
    /// Gets the name of the author that produced this block, when the model provides one.
    /// </summary>
    public string? AuthorName { get; internal set; }

    /// <summary>
    /// Registers a callback that runs whenever this block changes.
    /// </summary>
    /// <param name="callback">The callback to invoke when the block changes.</param>
    /// <returns>A subscription that removes the callback when disposed.</returns>
    public ContentBlockChangedSubscription OnChanged(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callbacks.Add(callback);
        return new ContentBlockChangedSubscription(this, callback);
    }

    /// <summary>
    /// Notifies subscribers that this block changed.
    /// </summary>
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
