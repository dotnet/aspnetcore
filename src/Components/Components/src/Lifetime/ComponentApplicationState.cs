using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    public class ComponentApplicationState
    {
        private IDictionary<string, byte[]>? _existingState;
        private readonly IDictionary<string, byte[]> _currentState;
        private readonly List<Func<Task>> _registeredCallbacks;

        internal ComponentApplicationState(
            IDictionary<string, byte[]> currentState,
            List<Func<Task>> pauseCallbacks)
        {
            _currentState = currentState;
            _registeredCallbacks = pauseCallbacks;
        }

        internal void InitializeExistingState(IDictionary<string, byte[]> existingState)
        {
            if (_existingState != null)
            {
                throw new InvalidOperationException("ComponentApplicationState already initialized.");
            }
            _existingState = existingState ?? throw new ArgumentNullException(nameof(existingState));
        }

        public void RegisterOnPersistingCallback(Func<Task> callback)
        {
            _registeredCallbacks.Add(callback);
        }

        public bool TryRetrievePersistedState(string key, [MaybeNullWhen(false)] out byte[] value)
        {
            if (_existingState == null)
            {
                throw new InvalidOperationException("ComponentApplicationState has not been initialized.");
            }

            return _existingState.TryGetValue(key, out value);
        }

        public void PersistState(string key, byte[] value)
        {
            _currentState[key] = value;
        }
    }

    public class ComponentApplicationLifetime
    {
        private bool _stateIsPersisted;
        private bool _pauseInProgress;
        private List<Func<Task>> _pauseCallbacks = new();
        private readonly Dictionary<string, byte[]> _currentState = new();

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
            foreach (var callback in _pauseCallbacks)
            {
                await callback();
            }
            _pauseInProgress = false;
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
            var data = new ReadOnlyDictionary<string, byte[]>(_currentState);
            return store.PersistStateAsync(data);
        }

        internal void AddRegistration(Func<Task> callback)
        {
            _pauseCallbacks.Add(callback);
        }
    }

    public interface IComponentApplicationStateStore
    {
        IDictionary<string, byte[]> GetPersistedState();

        Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> manager);
    }
}
