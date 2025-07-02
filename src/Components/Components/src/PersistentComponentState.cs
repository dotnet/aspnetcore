// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The state for the components and services of a components application.
/// </summary>
public class PersistentComponentState
{
    private IDictionary<string, byte[]>? _existingState;
    private readonly IDictionary<string, byte[]> _currentState;

    private readonly List<PersistComponentStateRegistration> _registeredCallbacks;
    private readonly List<RestoreComponentStateRegistration> _restoringCallbacks = new();

    internal PersistentComponentState(
        IDictionary<string , byte[]> currentState,
        List<PersistComponentStateRegistration> pauseCallbacks)
    {
        _currentState = currentState;
        _registeredCallbacks = pauseCallbacks;
    }

    internal bool PersistingState { get; set; }

    /// <summary>
    /// Gets the current restoration scenario, if any.
    /// </summary>
    public IPersistentComponentStateScenario? CurrentScenario { get; internal set; }

    internal void InitializeExistingState(IDictionary<string, byte[]> existingState)
    {
        if (_existingState != null)
        {
            throw new InvalidOperationException("PersistentComponentState already initialized.");
        }
        _existingState = existingState ?? throw new ArgumentNullException(nameof(existingState));
    }

    /// <summary>
    /// Register a callback to persist the component state when the application is about to be paused.
    /// Registered callbacks can use this opportunity to persist their state so that it can be retrieved when the application resumes.
    /// </summary>
    /// <param name="callback">The callback to invoke when the application is being paused.</param>
    /// <returns>A subscription that can be used to unregister the callback when disposed.</returns>
    public PersistingComponentStateSubscription RegisterOnPersisting(Func<Task> callback)
        => RegisterOnPersisting(callback, null);

    /// <summary>
    /// Register a callback to persist the component state when the application is about to be paused.
    /// Registered callbacks can use this opportunity to persist their state so that it can be retrieved when the application resumes.
    /// </summary>
    /// <param name="callback">The callback to invoke when the application is being paused.</param>
    /// <param name="renderMode"></param>
    /// <returns>A subscription that can be used to unregister the callback when disposed.</returns>
    public PersistingComponentStateSubscription RegisterOnPersisting(Func<Task> callback, IComponentRenderMode? renderMode)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (PersistingState)
        {
            throw new InvalidOperationException("Registering a callback while persisting state is not allowed.");
        }

        var persistenceCallback = new PersistComponentStateRegistration(callback, renderMode);

        _registeredCallbacks.Add(persistenceCallback);

        return new PersistingComponentStateSubscription(_registeredCallbacks, persistenceCallback);
    }

    /// <summary>
    /// Serializes <paramref name="instance"/> as JSON and persists it under the given <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="TValue">The <paramref name="instance"/> type.</typeparam>
    /// <param name="key">The key to use to persist the state.</param>
    /// <param name="instance">The instance to persist.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public void PersistAsJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, TValue instance)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!PersistingState)
        {
            throw new InvalidOperationException("Persisting state is only allowed during an OnPersisting callback.");
        }

        if (_currentState.ContainsKey(key))
        {
            throw new ArgumentException($"There is already a persisted object under the same key '{key}'");
        }

        _currentState.Add(key, JsonSerializer.SerializeToUtf8Bytes(instance, JsonSerializerOptionsProvider.Options));
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    internal void PersistAsJson(string key, object instance, [DynamicallyAccessedMembers(JsonSerialized)] Type type)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!PersistingState)
        {
            throw new InvalidOperationException("Persisting state is only allowed during an OnPersisting callback.");
        }

        if (_currentState.ContainsKey(key))
        {
            throw new ArgumentException($"There is already a persisted object under the same key '{key}'");
        }

        _currentState.Add(key, JsonSerializer.SerializeToUtf8Bytes(instance, type, JsonSerializerOptionsProvider.Options));
    }

    /// <summary>
    /// Tries to retrieve the persisted state as JSON with the given <paramref name="key"/> and deserializes it into an
    /// instance of type <typeparamref name="TValue"/>.
    /// When the key is present, the state is successfully returned via <paramref name="instance"/>
    /// and removed from the <see cref="PersistentComponentState"/>.
    /// </summary>
    /// <param name="key">The key used to persist the instance.</param>
    /// <param name="instance">The persisted instance.</param>
    /// <returns><c>true</c> if the state was found; <c>false</c> otherwise.</returns>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public bool TryTakeFromJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, [MaybeNullWhen(false)] out TValue? instance)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (TryTake(key, out var data))
        {
            var reader = new Utf8JsonReader(data);
            instance = JsonSerializer.Deserialize<TValue>(ref reader, JsonSerializerOptionsProvider.Options)!;
            return true;
        }
        else
        {
            instance = default;
            return false;
        }
    }

    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    internal bool TryTakeFromJson(string key, [DynamicallyAccessedMembers(JsonSerialized)] Type type, [MaybeNullWhen(false)] out object? instance)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(key);
        if (TryTake(key, out var data))
        {
            var reader = new Utf8JsonReader(data);
            instance = JsonSerializer.Deserialize(ref reader, type, JsonSerializerOptionsProvider.Options);
            return true;
        }
        else
        {
            instance = default;
            return false;
        }
    }

    /// <summary>
    /// Registers a callback to be invoked when state is restored and the filter allows the current scenario.
    /// </summary>
    /// <param name="filter">The filter to determine if the callback should be invoked for a scenario.</param>
    /// <param name="callback">The callback to invoke during restoration.</param>
    /// <returns>A subscription that can be disposed to unregister the callback.</returns>
    public RestoringComponentStateSubscription RegisterOnRestoring(
        IPersistentStateFilter filter,
        Action callback)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(callback);

        // Create a wrapper scenario that uses the filter
        var filterScenario = new FilterWrapperScenario(filter);
        var registration = new RestoreComponentStateRegistration(filterScenario, callback);
        _restoringCallbacks.Add(registration);

        // If we already have a current scenario and it matches, invoke immediately
        if (CurrentScenario != null && ShouldInvokeCallback(filterScenario, CurrentScenario))
        {
            callback();
        }

        return new RestoringComponentStateSubscription(_restoringCallbacks, filterScenario, callback);
    }

    /// <summary>
    /// A scenario wrapper that uses a filter to determine if it should match the current scenario.
    /// </summary>
    private sealed class FilterWrapperScenario : IPersistentComponentStateScenario
    {
        private readonly IPersistentStateFilter _filter;

        public FilterWrapperScenario(IPersistentStateFilter filter)
        {
            _filter = filter;
        }

        public bool IsRecurring => true; // Filter-based scenarios can be recurring

        public bool ShouldMatchScenario(IPersistentComponentStateScenario currentScenario)
        {
            return _filter.ShouldRestore(currentScenario);
        }

        public override bool Equals(object? obj)
        {
            return obj is FilterWrapperScenario other && ReferenceEquals(_filter, other._filter);
        }

        public override int GetHashCode() => _filter.GetHashCode();
    }

    /// <summary>
    /// Updates the existing state with new state for subsequent restoration calls.
    /// Only allowed when existing state is empty (fully consumed).
    /// </summary>
    /// <param name="newState">New state dictionary to replace existing state.</param>
    /// <param name="scenario">The restoration scenario context.</param>
    internal void UpdateExistingState(IDictionary<string, byte[]> newState, IPersistentComponentStateScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(newState);
        ArgumentNullException.ThrowIfNull(scenario);

        if (_existingState != null && _existingState.Count > 0)
        {
            throw new InvalidOperationException("Cannot update existing state when state dictionary is not empty. State must be fully consumed before updating.");
        }

        _existingState = newState;
        CurrentScenario = scenario;

        // Invoke matching restoration callbacks
        InvokeRestoringCallbacks(scenario);
    }

    private void InvokeRestoringCallbacks(IPersistentComponentStateScenario scenario)
    {
        for (int i = _restoringCallbacks.Count - 1; i >= 0; i--)
        {
            var registration = _restoringCallbacks[i];
            
            if (ShouldInvokeCallback(registration.Scenario, scenario))
            {
                registration.Callback();
                
                // Remove non-recurring callbacks after invocation
                if (!registration.Scenario.IsRecurring)
                {
                    _restoringCallbacks.RemoveAt(i);
                }
            }
        }
    }

    private static bool ShouldInvokeCallback(IPersistentComponentStateScenario callbackScenario, IPersistentComponentStateScenario currentScenario)
    {
        // Special handling for filter wrapper scenarios
        if (callbackScenario is FilterWrapperScenario filterWrapper)
        {
            return filterWrapper.ShouldMatchScenario(currentScenario);
        }

        // For regular scenarios, match by type and properties
        return callbackScenario.GetType() == currentScenario.GetType() &&
               callbackScenario.Equals(currentScenario);
    }

    private bool TryTake(string key, out byte[]? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_existingState == null)
        {
            // Services during prerendering might try to access their state upon injection on the page
            // and we don't want to fail in that case.
            // When a service is prerendering there is no state to restore and in other cases the host
            // is responsible for initializing the state before services or components can access it.
            value = default;
            return false;
        }

        if (_existingState.TryGetValue(key, out value))
        {
            _existingState.Remove(key);
            return true;
        }
        else
        {
            return false;
        }
    }
}
