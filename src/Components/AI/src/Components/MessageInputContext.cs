// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Provides the state and operations of a <see cref="MessageInput"/> to components rendered
/// inside it.
/// </summary>
public sealed class MessageInputContext
{
    private readonly MessageInput _owner;
    private readonly List<Action> _callbacks = [];

    internal MessageInputContext(MessageInput owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets or sets the text that will be included in the next message.
    /// </summary>
    public string Text
    {
        get => _owner.Text;
        set => _owner.SetText(value);
    }

    /// <summary>
    /// Gets the binary content attached to the next message.
    /// </summary>
    public IReadOnlyList<DataContent> Attachments => _owner.Attachments;

    /// <summary>
    /// Gets the current conversation status.
    /// </summary>
    public ConversationStatus Status => _owner.Status;

    /// <summary>
    /// Gets a value indicating whether the conversation is streaming or awaiting user input.
    /// </summary>
    public bool IsConversationBusy => _owner.IsConversationBusy;

    /// <summary>
    /// Gets a value indicating whether an attachment or recording operation is in progress.
    /// </summary>
    public bool IsComposing => _owner.IsComposing;

    /// <summary>
    /// Gets a value indicating whether the current text and attachments can be submitted.
    /// </summary>
    public bool CanSubmit => _owner.CanSubmit;

    /// <summary>
    /// Gets a value indicating whether the current response can be stopped.
    /// </summary>
    public bool CanCancel => _owner.CanCancel;

    /// <summary>
    /// Gets the latest non-error composer status message.
    /// </summary>
    public string? StatusMessage => _owner.StatusMessage;

    /// <summary>
    /// Gets the latest composer error message.
    /// </summary>
    public string? ErrorMessage => _owner.ErrorMessage;

    /// <summary>
    /// Adds binary content to the next message.
    /// </summary>
    /// <param name="content">The content to attach.</param>
    /// <returns>A task that completes when the attachment has been added.</returns>
    public ValueTask AddAttachmentAsync(DataContent content)
    {
        return _owner.AddAttachmentAsync(content);
    }

    /// <summary>
    /// Removes binary content from the next message.
    /// </summary>
    /// <param name="content">The content to remove.</param>
    /// <returns>A task that completes when the attachment has been removed.</returns>
    public ValueTask RemoveAttachmentAsync(DataContent content)
    {
        return _owner.RemoveAttachmentAsync(content);
    }

    /// <summary>
    /// Submits the current text and attachments.
    /// </summary>
    /// <returns>A task that completes when the response finishes.</returns>
    public Task SubmitAsync()
    {
        return _owner.SubmitAsync();
    }

    /// <summary>
    /// Stops the current response.
    /// </summary>
    /// <returns>A task that completes when the active response has stopped.</returns>
    public Task CancelAsync()
    {
        return _owner.CancelAsync();
    }

    /// <summary>
    /// Moves keyboard focus to the message text area.
    /// </summary>
    /// <returns>A task that completes after focus has been requested.</returns>
    public ValueTask FocusAsync()
    {
        return _owner.FocusAsync();
    }

    /// <summary>
    /// Updates the composer status presented to the user.
    /// </summary>
    /// <param name="message">The new status message.</param>
    public void SetStatusMessage(string? message)
    {
        _owner.SetStatusMessage(message);
    }

    /// <summary>
    /// Updates the composer error presented to the user.
    /// </summary>
    /// <param name="message">The new error message.</param>
    public void SetErrorMessage(string? message)
    {
        _owner.SetErrorMessage(message);
    }

    /// <summary>
    /// Registers a callback invoked when composer state changes.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <returns>A registration that removes the callback when disposed.</returns>
    public IDisposable RegisterOnChanged(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callbacks.Add(callback);
        return new ChangedSubscription(_callbacks, callback);
    }

    internal void SetComposing(bool value)
    {
        _owner.SetComposing(value);
    }

    internal void NotifyChanged()
    {
        foreach (var callback in _callbacks.ToArray())
        {
            callback();
        }
    }

    private sealed class ChangedSubscription(
        List<Action> callbacks,
        Action callback) : IDisposable
    {
        private List<Action>? _callbacks = callbacks;
        private Action? _callback = callback;

        public void Dispose()
        {
            if (_callbacks is not null && _callback is not null)
            {
                _callbacks.Remove(_callback);
                _callbacks = null;
                _callback = null;
            }
        }
    }
}
