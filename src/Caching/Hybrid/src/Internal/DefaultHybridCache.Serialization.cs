// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;
partial class DefaultHybridCache
{
    private readonly ConcurrentDictionary<Type, object> serializers = new(); // per instance cache of typed serializers

    internal int MaximumPayloadBytes { get; }

    internal IHybridCacheSerializer<T> GetSerializer<T>()
    {
        return serializers.TryGetValue(typeof(T), out var serializer)
            ? Unsafe.As<IHybridCacheSerializer<T>>(serializer) : ResolveAndAddSerializer(this);

        static IHybridCacheSerializer<T> ResolveAndAddSerializer(DefaultHybridCache @this)
        {
            // it isn't critical that we get only one serializer instance during start-up; what matters
            // is that we don't get a new serializer instance *every time*
            var serializer = @this.services.GetServices<IHybridCacheSerializer<T>>().LastOrDefault();
            if (serializer is null)
            {
                foreach (var factory in @this.serializerFactories)
                {
                    if (factory.TryCreateSerializer<T>(out var current))
                    {
                        serializer = current;
                        break; // we've already reversed the factories, so: the first hit is what we want
                    }
                }
            }
            if (serializer is null)
            {
                throw new InvalidOperationException($"No {nameof(IHybridCacheSerializer<T>)} configured for type '{typeof(T).Name}'");
            }
            // store the result so we don't repeat this in future
            @this.serializers[typeof(T)] = serializer;
            return serializer;
        }
    }

    private static class ImmutableTypeCache<T> // lazy memoize; T doesn't change per cache instance
    {
        public static readonly bool IsImmutable =
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            !RuntimeHelpers.IsReferenceOrContainsReferences<T>() || // a pure struct will be a full copy every time
#endif
            DefaultHybridCache.IsImmutable(typeof(T));
    }

    internal static bool IsImmutable(Type type)
    {
        if (type is null || type == typeof(string) || type.IsPrimitive)
        {
            return true; // trivial cases
        }

        if (Nullable.GetUnderlyingType(type) is { } nullable)
        {
            type = nullable; // from Foo? to Foo
        }

        return Attribute.IsDefined(type, typeof(ImmutableObjectAttribute));
    }
}
