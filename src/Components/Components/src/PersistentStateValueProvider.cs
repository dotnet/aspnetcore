// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal sealed partial class PersistentStateValueProvider(PersistentComponentState state, ILogger<PersistentStateValueProvider> logger, IServiceProvider serviceProvider) : ICascadingValueSupplier
{
    private static readonly ConcurrentDictionary<(string, string, string), byte[]> _keyCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyGetter> _propertyGetterCache = new();
    private static readonly ConcurrentDictionary<Type, IPersistentComponentStateSerializer?> _serializerCache = new();

    private readonly Dictionary<ComponentState, ComponentSubscription> _subscriptions = [];

    public bool IsFixed => false;
    // For testing purposes only
    internal Dictionary<ComponentState, ComponentSubscription> Subscriptions => _subscriptions;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is PersistentStateAttribute;

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2026:RequiresUnreferencedCode message",
        Justification = "JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
    {
        var componentState = (ComponentState)key!;

        if (_subscriptions.TryGetValue(componentState, out var subscription))
        {
            return subscription.LastValue;
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "OpenComponent already has the right set of attributes")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")]
    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var propertyName = parameterInfo.PropertyName;
        var propertyType = parameterInfo.PropertyType;
        var storageKey = ComputeKey(subscriber, propertyName);


        // Resolve serializer outside the lambda
        var customSerializer = _serializerCache.GetOrAdd(propertyType, SerializerFactory);

        var propertyGetter = ResolvePropertyGetter(subscriber.Component.GetType(), propertyName);

        var componentSubscription = new ComponentSubscription();
        _subscriptions[subscriber] = componentSubscription;

        var persistingSubscription = state.RegisterOnPersisting(() =>
            {
                var property = propertyGetter.GetValue(subscriber.Component);
                if (property == null)
                {
                    Log.SkippedPersistingNullValue(logger, storageKey, propertyType.Name, subscriber.Component.GetType().Name);
                    return Task.CompletedTask;
                }

                var storageKey = ComputeKey(subscriber, propertyName);

                if (customSerializer != null)
                {
                    Log.PersistingValueToState(logger, storageKey, propertyType.Name, subscriber.Component.GetType().Name);

                    using var writer = new PooledArrayBufferWriter<byte>();
                    customSerializer.Persist(propertyType, property, writer);
                    state.PersistAsBytes(storageKey, writer.WrittenMemory.ToArray());
                    return Task.CompletedTask;
                }

                // Fallback to JSON serialization
                Log.PersistingValueToState(logger, storageKey, propertyType.Name, subscriber.Component.GetType().Name);
                state.PersistAsJson(storageKey, property, propertyType);
                return Task.CompletedTask;
            }, subscriber.Renderer.GetComponentRenderMode(subscriber.Component));

        componentSubscription.SetPersistingSubscription(persistingSubscription);

        var filterAttributes = propertyGetter.PropertyInfo.GetCustomAttributes(inherit: true)
            .OfType<IPersistentStateFilter>();

        Action? restorationAction = null;

        foreach (var filter in filterAttributes)
        {
            restorationAction ??= CreateRestorationAction(storageKey, propertyType, componentSubscription);
            var restoringSubscription = state.RegisterOnRestoring(filter, restorationAction);
            componentSubscription.AddRestoringSubscription(restoringSubscription);
        }

        if (!componentSubscription.HasRestoringSubscriptions)
        {
            restorationAction ??= CreateRestorationAction(storageKey, propertyType, componentSubscription);
            var defaultRestoringSubscription = state.RegisterOnRestoring(null, restorationAction);
            componentSubscription.AddRestoringSubscription(defaultRestoringSubscription);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "OpenComponent already has the right set of attributes")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")]
    private Action CreateRestorationAction(string storageKey, [DynamicallyAccessedMembers(LinkerFlags.Component)] Type propertyType, ComponentSubscription componentSubscription)
    {
        return () =>
        {
            if (state.TryTakeFromJson(storageKey, propertyType, out var value))
            {
                Log.RestoringValueFromState(logger, storageKey, propertyType.Name);
                componentSubscription.SetLastValue(value);
            }
            else
            {
                Log.NoValueToRestoreFromState(logger, storageKey, propertyType.Name);
            }
        };
    }

    private static PropertyGetter ResolvePropertyGetter(Type type, string propertyName)
    {
        return _propertyGetterCache.GetOrAdd((type, propertyName), PropertyGetterFactory);
    }

    private IPersistentComponentStateSerializer? SerializerFactory(Type type)
    {
        var serializerType = typeof(PersistentComponentStateSerializer<>).MakeGenericType(type);
        var serializer = serviceProvider.GetService(serializerType);

        // The generic class now inherits from the internal interface, so we can cast directly
        return serializer as IPersistentComponentStateSerializer;
    }

    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2077:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The source field does not have matching annotations.",
    Justification = "Properties of rendered components are preserved through other means and won't get trimmed.")]

    private static PropertyGetter PropertyGetterFactory((Type type, string propertyName) key)
    {
        var (type, propertyName) = key;
        var propertyInfo = GetPropertyInfo(type, propertyName);
        if (propertyInfo == null)
        {
            throw new InvalidOperationException($"Property {propertyName} not found on type {type.FullName}");
        }
        return new PropertyGetter(type, propertyInfo);

        static PropertyInfo? GetPropertyInfo([DynamicallyAccessedMembers(LinkerFlags.Component)] Type type, string propertyName)
            => type.GetProperty(propertyName);
    }

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        if (_subscriptions.TryGetValue(subscriber, out var subscription))
        {
            subscription.Dispose();
            _subscriptions.Remove(subscriber);
        }
    }

    // Internal for testing only
    internal static string ComputeKey(ComponentState componentState, string propertyName)
    {
        // We need to come up with a pseudo-unique key for the storage key.
        // We need to consider the property name, the component type, and its position within the component tree.
        // If only one component of a given type is present on the page, then only the component type + property name is enough.
        // If multiple components of the same type are present on the page, then we need to consider the position within the tree.
        // To do that, we are going to use the `@key` directive on the component if present and if we deem it serializable.
        // Serializable keys are Guid, DateOnly, TimeOnly, and any primitive type.
        // The key is composed of four segments:
        // Parent component type
        // Component type
        // Property name
        // @key directive if present and serializable.
        // We combine the first three parts into an identifier, and then we generate a derived identifier with the key
        // We do it this way becasue the information for the first three pieces of data is static for the lifetime of the
        // program and can be cached on each situation.

        var parentComponentType = GetParentComponentType(componentState);
        var componentType = GetComponentType(componentState);

        var preKey = _keyCache.GetOrAdd((parentComponentType, componentType, propertyName), KeyFactory);
        var finalKey = ComputeFinalKey(preKey, componentState);

        return finalKey;
    }

    private static string ComputeFinalKey(byte[] preKey, ComponentState componentState)
    {
        Span<byte> keyHash = stackalloc byte[SHA256.HashSizeInBytes];

        var key = GetSerializableKey(componentState);
        byte[]? pool = null;
        try
        {
            Span<byte> keyBuffer = stackalloc byte[1024];
            var currentBuffer = keyBuffer;
            preKey.CopyTo(keyBuffer);
            if (key is IUtf8SpanFormattable spanFormattable)
            {
                var wroteKey = false;
                while (!wroteKey)
                {
                    currentBuffer = keyBuffer[preKey.Length..];
                    wroteKey = spanFormattable.TryFormat(currentBuffer, out var written, "", CultureInfo.InvariantCulture);
                    if (!wroteKey)
                    {
                        // It is really unlikely that we will enter here, but we need to handle this case
                        Debug.Assert(written == 0);
                        GrowBuffer(ref pool, ref keyBuffer);
                    }
                    else
                    {
                        currentBuffer = currentBuffer[..written];
                    }
                }
            }
            else
            {
                var keySpan = ResolveKeySpan(key);
                var wroteKey = false;
                while (!wroteKey)
                {
                    currentBuffer = keyBuffer[preKey.Length..];
                    wroteKey = Encoding.UTF8.TryGetBytes(keySpan, currentBuffer, out var written);
                    if (!wroteKey)
                    {
                        // It is really unlikely that we will enter here, but we need to handle this case
                        Debug.Assert(written == 0);
                        // Since this is utf-8, grab a buffer the size of the key * 4 + the preKey size
                        // this guarantees we have enough space to encode the key
                        GrowBuffer(ref pool, ref keyBuffer, keySpan.Length * 4 + preKey.Length);
                    }
                    else
                    {
                        currentBuffer = currentBuffer[..written];
                    }
                }
            }

            keyBuffer = keyBuffer[..(preKey.Length + currentBuffer.Length)];

            var hashSucceeded = SHA256.TryHashData(keyBuffer, keyHash, out _);
            Debug.Assert(hashSucceeded);
            return Convert.ToBase64String(keyHash);
        }
        finally
        {
            if (pool != null)
            {
                ArrayPool<byte>.Shared.Return(pool, clearArray: true);
            }
        }
    }

    private static ReadOnlySpan<char> ResolveKeySpan(object? key)
    {
        if (key is IFormattable formattable)
        {
            var keyString = formattable.ToString("", CultureInfo.InvariantCulture);
            return keyString.AsSpan();
        }
        else if (key is IConvertible convertible)
        {
            var keyString = convertible.ToString(CultureInfo.InvariantCulture);
            return keyString.AsSpan();
        }
        return default;
    }

    private static void GrowBuffer(ref byte[]? pool, ref Span<byte> keyBuffer, int? size = null)
    {
        var newPool = pool == null ? ArrayPool<byte>.Shared.Rent(size ?? 2048) : ArrayPool<byte>.Shared.Rent(pool.Length * 2);
        keyBuffer.CopyTo(newPool);
        keyBuffer = newPool;
        if (pool != null)
        {
            ArrayPool<byte>.Shared.Return(pool, clearArray: true);
        }
        pool = newPool;
    }

    private static object? GetSerializableKey(ComponentState componentState)
    {
        var componentKey = componentState.GetComponentKey();
        if (componentKey != null && IsSerializableKey(componentKey))
        {
            return componentKey;
        }

        return null;
    }

    private static string GetComponentType(ComponentState componentState) => componentState.Component.GetType().FullName!;

    private static string GetParentComponentType(ComponentState componentState)
    {
        if (componentState.ParentComponentState == null)
        {
            return "";
        }
        if (componentState.ParentComponentState.Component == null)
        {
            return "";
        }

        if (componentState.ParentComponentState.ParentComponentState != null)
        {
            var renderer = componentState.Renderer;
            var parentRenderMode = renderer.GetComponentRenderMode(componentState.ParentComponentState.Component);
            var grandParentRenderMode = renderer.GetComponentRenderMode(componentState.ParentComponentState.ParentComponentState.Component);
            if (parentRenderMode != grandParentRenderMode)
            {
                // This is the case when EndpointHtmlRenderer introduces an SSRRenderBoundary component.
                // We want to return "" because the SSRRenderBoundary component is not a real component
                // and won't appear on the component tree in the WebAssemblyRenderer and RemoteRenderer
                // interactive scenarios.
                return "";
            }
        }

        return GetComponentType(componentState.ParentComponentState);
    }

    private static byte[] KeyFactory((string parentComponentType, string componentType, string propertyName) parts) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(".", parts.parentComponentType, parts.componentType, parts.propertyName)));

    private static bool IsSerializableKey(object key)
    {
        if (key == null)
        {
            return false;
        }
        var keyType = key.GetType();
        var result = Type.GetTypeCode(keyType) != TypeCode.Object
            || keyType == typeof(Guid)
            || keyType == typeof(DateTimeOffset)
            || keyType == typeof(DateOnly)
            || keyType == typeof(TimeOnly);

        return result;
    }

    /// <summary>
    /// Serializes <paramref name="instance"/> using the provided <paramref name="serializer"/> and persists it under the given <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="TValue">The <paramref name="instance"/> type.</typeparam>
    /// <param name="key">The key to use to persist the state.</param>
    /// <param name="instance">The instance to persist.</param>
    /// <param name="serializer">The custom serializer to use for serialization.</param>
    internal void PersistAsync<TValue>(string key, TValue instance, PersistentComponentStateSerializer<TValue> serializer)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(serializer);

        using var writer = new PooledArrayBufferWriter<byte>();
        serializer.Persist(instance, writer);
        state.PersistAsBytes(key, writer.WrittenMemory.ToArray());
    }

    /// <summary>
    /// Tries to retrieve the persisted state with the given <paramref name="key"/> and deserializes it using the provided <paramref name="serializer"/> into an
    /// instance of type <typeparamref name="TValue"/>.
    /// When the key is present, the state is successfully returned via <paramref name="instance"/>
    /// and removed from the <see cref="PersistentComponentState"/>.
    /// </summary>
    /// <param name="key">The key used to persist the instance.</param>
    /// <param name="serializer">The custom serializer to use for deserialization.</param>
    /// <param name="instance">The persisted instance.</param>
    /// <returns><c>true</c> if the state was found; <c>false</c> otherwise.</returns>
    internal bool TryTake<TValue>(string key, PersistentComponentStateSerializer<TValue> serializer, [MaybeNullWhen(false)] out TValue instance)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(serializer);

        if (state.TryTakeBytes(key, out var data))
        {
            var sequence = new ReadOnlySequence<byte>(data!);
            instance = serializer.Restore(sequence);
            return true;
        }
        else
        {
            instance = default;
            return false;
        }
    }

    internal class ComponentSubscription
    {
        private PersistingComponentStateSubscription? _persistingSubscription;
        private RestoringComponentStateSubscription? _firstRestoringSubscription;
        private List<RestoringComponentStateSubscription>? _additionalRestoringSubscriptions;

        public object? LastValue { get; private set; }

        public void SetPersistingSubscription(PersistingComponentStateSubscription subscription)
        {
            _persistingSubscription = subscription;
        }

        public void AddRestoringSubscription(RestoringComponentStateSubscription subscription)
        {
            if (_firstRestoringSubscription == null)
            {
                _firstRestoringSubscription = subscription;
            }
            else
            {
                _additionalRestoringSubscriptions ??= new List<RestoringComponentStateSubscription>();
                _additionalRestoringSubscriptions.Add(subscription);
            }
        }

        public bool HasRestoringSubscriptions => _firstRestoringSubscription != null;

        public void SetLastValue(object? value)
        {
            LastValue = value;
        }

        public void Dispose()
        {
            _persistingSubscription?.Dispose();
            _firstRestoringSubscription?.Dispose();
            if (_additionalRestoringSubscriptions != null)
            {
                foreach (var subscription in _additionalRestoringSubscriptions)
                {
                    subscription.Dispose();
                }
                _additionalRestoringSubscriptions.Clear();
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Persisting value for storage key '{StorageKey}' of type '{PropertyType}' from component '{ComponentType}'", EventName = "PersistingValueToState")]
        public static partial void PersistingValueToState(ILogger logger, string storageKey, string propertyType, string componentType);

        [LoggerMessage(2, LogLevel.Debug, "Skipped persisting null value for storage key '{StorageKey}' of type '{PropertyType}' from component '{ComponentType}'", EventName = "SkippedPersistingNullValue")]
        public static partial void SkippedPersistingNullValue(ILogger logger, string storageKey, string propertyType, string componentType);

        [LoggerMessage(3, LogLevel.Debug, "Restoring value for storage key '{StorageKey}' of type '{PropertyType}'", EventName = "RestoringValueFromState")]
        public static partial void RestoringValueFromState(ILogger logger, string storageKey, string propertyType);

        [LoggerMessage(4, LogLevel.Debug, "No value to restore for storage key '{StorageKey}' of type '{PropertyType}'", EventName = "NoValueToRestoreFromState")]
        public static partial void NoValueToRestoreFromState(ILogger logger, string storageKey, string propertyType);

        [LoggerMessage(5, LogLevel.Debug, "Restored value from persistent state for storage key '{StorageKey}' of type '{PropertyType}' for component '{ComponentType}'", EventName = "RestoredValueFromPersistentState")]
        public static partial void RestoredValueFromPersistentState(ILogger logger, string storageKey, string propertyType, string componentType);

        [LoggerMessage(6, LogLevel.Debug, "Value not found in persistent state for storage key '{StorageKey}' of type '{PropertyType}' for component '{ComponentType}'", EventName = "ValueNotFoundInPersistentState")]
        public static partial void ValueNotFoundInPersistentState(ILogger logger, string storageKey, string propertyType, string componentType);
    }
}
