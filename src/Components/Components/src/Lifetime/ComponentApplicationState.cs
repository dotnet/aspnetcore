using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    public class ComponentApplicationState
    {
        private IDictionary<string, string>? _existingState;
        private readonly IDictionary<string, string> _currentState;
        private readonly IDictionary<object, Func<Task>> _registeredCallbacks;

        internal ComponentApplicationState(
            IDictionary<string, string> currentState,
            IDictionary<object, Func<Task>> pauseCallbacks)
        {
            _currentState = currentState;
            _registeredCallbacks = pauseCallbacks;
        }

        internal void InitializeExistingState(IDictionary<string, string> existingState)
        {
            if (_existingState != null)
            {
                throw new InvalidOperationException("ComponentApplicationState already initialized.");
            }
            _existingState = existingState ?? throw new ArgumentNullException(nameof(existingState));
        }

        public void RegisterOnPersistingCallback(Func<Task> callback, object instance)
        {
            _registeredCallbacks.Add(instance, callback);
        }

        public bool TryRetrievePersistedState(string key, [MaybeNullWhen(false)] out string value)
        {
            if (_existingState == null)
            {
                throw new InvalidOperationException("ComponentApplicationState has not been initialized.");
            }

            return _existingState.TryGetValue(key, out value);
        }

        public void PersistState(string key, string value)
        {
            _currentState[key] = value;
        }
    }

    public class ComponentApplicationLifetime
    {
        private bool _stateIsPersisted;
        private bool _pauseInProgress;
        private Dictionary<object, Func<Task>> _pauseCallbacks = new();
        private readonly Dictionary<string, string> _currentState = new();

        public ComponentApplicationLifetime()
        {
            State = new ComponentApplicationState(_currentState, _pauseCallbacks);
        }

        public ComponentApplicationState State { get; }

        public Task RestoreStateAsync(IComponentApplicationStateStore store)
        {
            var data = store.GetPersistedState();
            State.InitializeExistingState(data);

            return Task.CompletedTask;
        }

        public async Task PauseAsync()
        {
            _pauseInProgress = true;
            foreach (var (instance, callback) in _pauseCallbacks)
            {
                await callback();
            }
            _pauseInProgress = false;
        }

        public async Task PauseAsync(object instance)
        {
            if (!_pauseCallbacks.TryGetValue(instance, out var callback))
            {
                return;
            }
            else
            {
                _pauseCallbacks.Remove(instance);

                _pauseInProgress = true;
                await callback();
                _pauseInProgress = false;
            }
        }

        public Task PersistStateAsync(IComponentApplicationStateStore store)
        {
            if (State == null)
            {
                throw new InvalidOperationException("ComponentApplicationLifetimeNotInitialized.");
            }
            if (_stateIsPersisted)
            {
                throw new InvalidOperationException("State already persisted.");
            }
            if (_pauseInProgress)
            {
                throw new InvalidOperationException("A pause operation is in progress.");
            }
            if (_pauseCallbacks.Count > 0)
            {
                throw new InvalidOperationException("Not all registered instances have been paused.");
            }
            _stateIsPersisted = true;
            var data = new ReadOnlyDictionary<string, string>(_currentState);
            return store.PersistStateAsync(data);
        }

        internal void AddRegistration(Func<Task> callback, object instance)
        {
            _pauseCallbacks.Add(instance, callback);
        }
    }

    public interface IComponentApplicationStateStore
    {
        IDictionary<string, string> GetPersistedState();

        Task PersistStateAsync(IReadOnlyDictionary<string, string> manager);
    }
}
