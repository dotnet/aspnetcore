// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal partial class PersistentValueProviderComponentSubscription : IDisposable
{
    private static readonly object _uninitializedSentinel = new();

    private readonly PersistentComponentState _state;
    private readonly ComponentState _subscriber;
    private readonly string _propertyName;
    private readonly Type _propertyType;
    private readonly Func<object, object?> _propertyGetter;
    private readonly IPersistentComponentStateSerializer? _customSerializer;
    private readonly ILogger _logger;

    private readonly PersistingComponentStateSubscription? _persistingSubscription;
    private readonly RestoringComponentStateSubscription? _restoringSubscription;
    private object? _lastValue = _uninitializedSentinel;
    private bool _hasPendingInitialValue;
    private bool _ignoreComponentPropertyValue;
    private string? _storageKey;

    public PersistentValueProviderComponentSubscription(
        PersistentComponentState state,
        ComponentState subscriber,
        CascadingParameterInfo parameterInfo,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _state = state;
        _subscriber = subscriber;
        _propertyName = parameterInfo.PropertyName;
        _propertyType = parameterInfo.PropertyType;
        _logger = logger;
        var attribute = (PersistentStateAttribute)parameterInfo.Attribute;

        var parameter = ResolveParameter(subscriber.ComponentTypeInfo, _propertyName);
        _customSerializer = parameter.GetStateSerializer?.Invoke(serviceProvider) as IPersistentComponentStateSerializer;
        _propertyGetter = parameter.GetValue;

        _persistingSubscription = state.RegisterOnPersisting(
            PersistProperty,
            subscriber.Renderer.GetComponentRenderMode(subscriber.Component));

        _restoringSubscription = state.RegisterOnRestoring(
            RestoreProperty,
            new RestoreOptions { RestoreBehavior = attribute.RestoreBehavior, AllowUpdates = attribute.AllowUpdates });
    }

    // GetOrComputeLastValue is a bit of a special provider.
    // Right after a Restore operation it will capture the last value and return that, but it must support the user
    // overriding the property at a later point, so to support that, we need to keep track of whether or not we have
    // delivered the last value, and if so, instead of returning the _lastValue, we simply read the property and return
    // that instead. That way, if the component updates the property in SetParametersAsync, we won't revert it to the
    // value we restored from the persistent state.
    internal object? GetOrComputeLastValue()
    {
        var isInitialized = !ReferenceEquals(_lastValue, _uninitializedSentinel);
        if (!isInitialized)
        {
            // Remove the uninitialized sentinel.
            _lastValue = null;
            if (_hasPendingInitialValue)
            {
                RestoreProperty();
                _hasPendingInitialValue = false;
            }
        }
        else
        {
            if (_ignoreComponentPropertyValue)
            {
                // At this point, we just received a value update from `RestoreProperty`.
                // The property value might have been modified by the component and in this
                // case we want to overwrite it with the value we just restored.
                _ignoreComponentPropertyValue = false;
                return _lastValue;
            }
            else
            {
                // In this case, the component might have modified the property value after
                // we restored it from the persistent state. We don't want to overwrite it
                // with a previously restored value.
                var currentPropertyValue = _propertyGetter(_subscriber.Component);
                return currentPropertyValue;
            }
        }

        return _lastValue;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.",
        Justification = "Persistent state JSON contracts are supplied through the framework serialization options.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2077:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "Persistent state JSON contracts are supplied through the framework serialization options.")]
    internal void RestoreProperty()
    {
        var skipNotifications = _hasPendingInitialValue;
        if (ReferenceEquals(_lastValue, _uninitializedSentinel) && !_hasPendingInitialValue)
        {
            // Upon subscribing, the callback might be invoked right away,
            // but this is too early to restore the first value since the component state
            // hasn't been fully initialized yet.
            // For that reason, we make a mark to restore the state on GetOrComputeLastValue.
            _hasPendingInitialValue = true;
            return;
        }

        // The key needs to be computed here, do not move this outside of the lambda.
        _storageKey ??= PersistentStateValueProviderKeyResolver.ComputeKey(_subscriber, _propertyName);

        if (_customSerializer is not null)
        {
            if (_state.TryTakeBytes(_storageKey, out var data))
            {
                Log.RestoringValueFromState(_logger, _storageKey, _propertyType.Name, _propertyName);
                var sequence = new ReadOnlySequence<byte>(data!);
                _lastValue = _customSerializer.Restore(_propertyType, sequence);
                _ignoreComponentPropertyValue = true;
                if (!skipNotifications)
                {
                    _subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
                }
            }
            else
            {
                Log.ValueNotFoundInPersistentState(_logger, _storageKey, _propertyType.Name, "null", _propertyName);
            }
        }
        else
        {
            if (_state.TryTakeFromJson(_storageKey, _propertyType, out var value))
            {
                Log.RestoredValueFromPersistentState(_logger, _storageKey, _propertyType.Name, "null", _propertyName);
                _lastValue = value;
                _ignoreComponentPropertyValue = true;
                if (!skipNotifications)
                {
                    _subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
                }
            }
            else
            {
                Log.NoValueToRestoreFromState(_logger, _storageKey, _propertyType.Name, _propertyName);
            }
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.",
        Justification = "Persistent state JSON contracts are supplied through the framework serialization options.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2077:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "Persistent state JSON contracts are supplied through the framework serialization options.")]
    private Task PersistProperty()
    {
        // The key needs to be computed here, do not move this outside of the lambda.
        _storageKey ??= PersistentStateValueProviderKeyResolver.ComputeKey(_subscriber, _propertyName);

        var property = _propertyGetter(_subscriber.Component);
        if (property is null)
        {
            Log.SkippedPersistingNullValue(_logger, _storageKey, _propertyType.Name, _subscriber.Component.GetType().Name, _propertyName);
            return Task.CompletedTask;
        }

        if (_customSerializer is not null)
        {
            Log.PersistingValueToState(_logger, _storageKey, _propertyType.Name, _subscriber.Component.GetType().Name, _propertyName);

            using var writer = new PooledArrayBufferWriter<byte>();
            _customSerializer.Persist(_propertyType, property, writer);
            _state.PersistAsBytes(_storageKey, writer.WrittenMemory.ToArray());
            return Task.CompletedTask;
        }

        // Fallback to JSON serialization
        Log.PersistingValueToState(_logger, _storageKey, _propertyType.Name, _subscriber.Component.GetType().Name, _propertyName);
        _state.PersistAsJson(_storageKey, property, _propertyType);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _persistingSubscription?.Dispose();
        _restoringSubscription?.Dispose();
    }

    private static ComponentParameterDescriptor ResolveParameter(
        ComponentTypeInfo typeInfo,
        string propertyName)
    {
        if (!typeInfo.IsParameterPubliclyReadable(propertyName))
        {
            throw new InvalidOperationException(
                $"A public property '{propertyName}' on component type '{typeInfo.Type.FullName}' with a public getter wasn't found.");
        }

        foreach (var parameter in typeInfo.Parameters)
        {
            if (string.Equals(parameter.Name, propertyName, StringComparison.Ordinal))
            {
                return parameter;
            }
        }

        throw new InvalidOperationException(
            $"A public property '{propertyName}' on component type '{typeInfo.Type.FullName}' with a public getter wasn't found.");
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Persisting value for storage key '{StorageKey}' of type '{PropertyType}' from component '{ComponentType}' for property '{PropertyName}'", EventName = "PersistingValueToState")]
        public static partial void PersistingValueToState(ILogger logger, string storageKey, string propertyType, string componentType, string propertyName);

        [LoggerMessage(2, LogLevel.Debug, "Skipped persisting null value for storage key '{StorageKey}' of type '{PropertyType}' from component '{ComponentType}' for property '{PropertyName}'", EventName = "SkippedPersistingNullValue")]
        public static partial void SkippedPersistingNullValue(ILogger logger, string storageKey, string propertyType, string componentType, string propertyName);

        [LoggerMessage(3, LogLevel.Debug, "Restoring value for storage key '{StorageKey}' of type '{PropertyType}' for property '{PropertyName}'", EventName = "RestoringValueFromState")]
        public static partial void RestoringValueFromState(ILogger logger, string storageKey, string propertyType, string propertyName);

        [LoggerMessage(4, LogLevel.Debug, "No value to restore for storage key '{StorageKey}' of type '{PropertyType}' for property '{PropertyName}'", EventName = "NoValueToRestoreFromState")]
        public static partial void NoValueToRestoreFromState(ILogger logger, string storageKey, string propertyType, string propertyName);

        [LoggerMessage(5, LogLevel.Debug, "Restored value from persistent state for storage key '{StorageKey}' of type '{PropertyType}' for component '{ComponentType}' for property '{PropertyName}'", EventName = "RestoredValueFromPersistentState")]
        public static partial void RestoredValueFromPersistentState(ILogger logger, string storageKey, string propertyType, string componentType, string propertyName);

        [LoggerMessage(6, LogLevel.Debug, "Value not found in persistent state for storage key '{StorageKey}' of type '{PropertyType}' for component '{ComponentType}' for property '{PropertyName}'", EventName = "ValueNotFoundInPersistentState")]
        public static partial void ValueNotFoundInPersistentState(ILogger logger, string storageKey, string propertyType, string componentType, string propertyName);
    }
}
