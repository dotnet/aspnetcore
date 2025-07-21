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
    private readonly List<RestoreComponentStateRegistration> _registeredRestoringCallbacks;
    private IPersistentComponentStateScenario? _currentScenario;

    internal PersistentComponentState(
        IDictionary<string, byte[]> currentState,
        List<PersistComponentStateRegistration> pauseCallbacks,
        List<RestoreComponentStateRegistration> restoringCallbacks)
    {
        _currentState = currentState;
        _registeredCallbacks = pauseCallbacks;
        _registeredRestoringCallbacks = restoringCallbacks;
    }

    internal bool PersistingState { get; set; }

    internal void InitializeExistingState(IDictionary<string, byte[]> existingState, IPersistentComponentStateScenario? scenario = null)
    {
        if (_existingState != null)
        {
            throw new InvalidOperationException("PersistentComponentState already initialized.");
        }
        _existingState = existingState ?? throw new ArgumentNullException(nameof(existingState));
        _currentScenario = scenario;
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
    /// Register a callback to restore the component state during specific scenarios.
    /// The callback will only be invoked if the filter supports the current restoration scenario.
    /// </summary>
    /// <param name="filter">The filter that determines when this callback should be invoked.</param>
    /// <param name="callback">The callback to invoke during state restoration.</param>
    /// <returns>A subscription that can be used to unregister the callback when disposed.</returns>
    public RestoringComponentStateSubscription RegisterOnRestoring(IPersistentStateFilter? filter, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_currentScenario == null || filter == null ||
            (filter != null && (!filter.SupportsScenario(_currentScenario) ||
            filter.ShouldRestore(_currentScenario))))
        {
            callback();
            return default;
        }

        if (_currentScenario is { IsRecurring: false })
        {
            return default;
        }
        else
        {
            var registration = new RestoreComponentStateRegistration(filter, callback);
            _registeredRestoringCallbacks.Add(registration);
            return new RestoringComponentStateSubscription(_registeredRestoringCallbacks, registration);
        }
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
    /// Persists the provided byte array under the given key.
    /// </summary>
    /// <param name="key">The key to use to persist the state.</param>
    /// <param name="data">The byte array to persist.</param>
    internal void PersistAsBytes(string key, byte[] data)
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

        _currentState.Add(key, data);
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
    /// Tries to retrieve the persisted state as raw bytes with the given <paramref name="key"/>.
    /// When the key is present, the raw bytes are successfully returned via <paramref name="data"/>
    /// and removed from the <see cref="PersistentComponentState"/>.
    /// </summary>
    /// <param name="key">The key used to persist the data.</param>
    /// <param name="data">The persisted raw bytes.</param>
    /// <returns><c>true</c> if the state was found; <c>false</c> otherwise.</returns>
    internal bool TryTakeBytes(string key, [MaybeNullWhen(false)] out byte[]? data)
    {
        return TryTake(key, out data);
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

    /// <summary>
    /// Updates the existing state with the given <paramref name="state"/> dictionary.
    /// </summary>
    /// <param name="state">The new state to apply.</param>
    /// <param name="scenario">The scenario for which the state is being updated.</param>
    internal void UpdateExistingState(IDictionary<string, byte[]> state, IPersistentComponentStateScenario? scenario)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_existingState == null || _existingState.Count > 0)
        {
            throw new InvalidOperationException("Cannot update existing state: previous state has not been cleared or state is not initialized.");
        }

        _existingState = state;
        _currentScenario = scenario;
    }
}
