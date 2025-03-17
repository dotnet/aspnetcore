// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components;

internal sealed class SupplyParameterFromPersistentComponentStateValueProvider(PersistentComponentState state) : ICascadingValueSupplier
{
    private static readonly ConcurrentDictionary<(string, string, string), byte[]> _keyCache = new();
    private static readonly ConcurrentDictionary<(Type, string), PropertyGetter> _propertyGetterCache = new();

    private readonly Dictionary<ComponentState, PersistingComponentStateSubscription> _subscriptions = [];

    public bool IsFixed => false;
    // For testing purposes only
    internal Dictionary<ComponentState, PersistingComponentStateSubscription> Subscriptions => _subscriptions;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromPersistentComponentStateAttribute;

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
        var storageKey = ComputeKey(componentState, parameterInfo.PropertyName);

        return state.TryTakeFromJson(storageKey, parameterInfo.PropertyType, out var value) ? value : null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")] [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "OpenComponent already has the right set of attributes")] [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")]
    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var propertyName = parameterInfo.PropertyName;
        var propertyType = parameterInfo.PropertyType;
        _subscriptions[subscriber] = state.RegisterOnPersisting(() =>
            {
                var storageKey = ComputeKey(subscriber, propertyName);
                var propertyGetter = ResolvePropertyGetter(subscriber.Component.GetType(), propertyName);
                var property = propertyGetter.GetValue(subscriber.Component);
                if (property == null)
                {
                    return Task.CompletedTask;
                }
                state.PersistAsJson(storageKey, property, propertyType);
                return Task.CompletedTask;
            }, subscriber.Renderer.GetComponentRenderMode(subscriber.Component));
    }

    private static PropertyGetter ResolvePropertyGetter(Type type, string propertyName)
    {
        return _propertyGetterCache.GetOrAdd((type, propertyName), PropertyGetterFactory);
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
        if (componentState.ParentComponentState is not { } parentComponentState)
        {
            return null;
        }

        // Check if the parentComponentState has a `@key` directive applied to the current component.
        var frames = parentComponentState.CurrentRenderTree.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            ref var currentFrame = ref frames.Array[i];
            if (currentFrame.FrameType != RenderTree.RenderTreeFrameType.Component ||
                !ReferenceEquals(componentState.Component, currentFrame.Component))
            {
                // Skip any frame that is not the current component.
                continue;
            }

            var componentKey = currentFrame.ComponentKey;
            return !IsSerializableKey(componentKey) ? null : componentKey;
        }

        return null;
    }

    private static string GetComponentType(ComponentState componentState) => componentState.Component.GetType().FullName!;

    private static string GetParentComponentType(ComponentState componentState) =>
        componentState.ParentComponentState == null ? "" : GetComponentType(componentState.ParentComponentState);

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
}
