// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class AgentState<T> where T : class, new()
{
    private T _value;
    private readonly List<Action> _callbacks = new();

    internal AgentState(T? initialValue = null)
    {
        _value = initialValue ?? new T();
    }

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            NotifyChanged();
        }
    }

    public IDisposable OnChanged(Action callback)
    {
        _callbacks.Add(callback);
        return new CallbackRegistration(_callbacks, callback);
    }

    internal void NotifyChanged()
    {
        var snapshot = _callbacks.ToArray();
        foreach (var cb in snapshot)
        {
            cb();
        }
    }

    private sealed class CallbackRegistration : IDisposable
    {
        private List<Action>? _list;
        private Action? _callback;

        internal CallbackRegistration(List<Action> list, Action callback)
        {
            _list = list;
            _callback = callback;
        }

        public void Dispose()
        {
            if (_list is not null && _callback is not null)
            {
                _list.Remove(_callback);
                _list = null;
                _callback = null;
            }
        }
    }
}
