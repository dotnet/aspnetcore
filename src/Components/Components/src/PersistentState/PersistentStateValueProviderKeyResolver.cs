// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class PersistentStateValueProviderKeyResolver
{
    private static readonly ConcurrentDictionary<(string, string, string), byte[]> _keyCache = new();

    static PersistentStateValueProviderKeyResolver()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCaches;
        }
    }

    private static void ClearCaches()
    {
        _keyCache.Clear();
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
        // We do it this way because the information for the first three pieces of data is static for the lifetime of the
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
}
