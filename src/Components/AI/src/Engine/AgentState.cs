// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Holds observable state associated with a <see cref="UIAgent{TState}"/>.
/// </summary>
/// <typeparam name="T">The type of state.</typeparam>
public class AgentState<T> where T : class, new()
{
    private readonly List<Action> _callbacks = new();
    private T _value;

    internal AgentState(T? initialValue = null)
    {
        _value = initialValue ?? new T();
    }

    /// <summary>
    /// Gets or sets the current state value.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _value = value;
            NotifyChanged();
        }
    }

    /// <summary>
    /// Registers a callback invoked when <see cref="Value"/> changes.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <returns>A registration that removes the callback when disposed.</returns>
    public IDisposable OnChanged(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callbacks.Add(callback);
        return new CallbackRegistration(_callbacks, callback);
    }

    private void NotifyChanged()
    {
        var snapshot = _callbacks.ToArray();
        foreach (var callback in snapshot)
        {
            callback();
        }
    }

    private sealed class CallbackRegistration : IDisposable
    {
        private List<Action>? _callbacks;
        private Action? _callback;

        internal CallbackRegistration(List<Action> callbacks, Action callback)
        {
            _callbacks = callbacks;
            _callback = callback;
        }

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
