// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;
partial class DefaultHybridCache
{
    // per instance cache of typed serializers; each serializer is a
    // IHybridCacheSerializer<T> for the corresponding Type, but we can't
    // know which here - and undesirable to add an artificial non-generic
    // IHybridCacheSerializer base that serves no other purpose
    private readonly ConcurrentDictionary<Type, object> _serializers = new();

    internal int MaximumPayloadBytes { get; }

    internal IHybridCacheSerializer<T> GetSerializer<T>()
    {
        return _serializers.TryGetValue(typeof(T), out var serializer)
            ? Unsafe.As<IHybridCacheSerializer<T>>(serializer) : ResolveAndAddSerializer(this);

        static IHybridCacheSerializer<T> ResolveAndAddSerializer(DefaultHybridCache @this)
        {
            // it isn't critical that we get only one serializer instance during start-up; what matters
            // is that we don't get a new serializer instance *every time*
            var serializer = @this._services.GetService<IHybridCacheSerializer<T>>();
            if (serializer is null)
            {
                foreach (var factory in @this._serializerFactories)
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
            @this._serializers[typeof(T)] = serializer;
            return serializer;
        }
    }

    internal static class ImmutableTypeCache<T> // lazy memoize; T doesn't change per cache instance
    {
        // note for blittable types: a pure struct will be a full copy every time - nothing shared to mutate
        public static readonly bool IsImmutable = (typeof(T).IsValueType && IsBlittable<T>()) || IsImmutable(typeof(T));
    }

    private static bool IsBlittable<T>() // minimize the generic portion
    {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return !RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
        try // down-level: only blittable types can be pinned
        {
            // get a typed, zeroed, non-null boxed instance of the appropriate type
            // (can't use (object)default(T), as that would box to null for nullable types)
            var obj = FormatterServices.GetUninitializedObject(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            GCHandle.Alloc(obj, GCHandleType.Pinned).Free();
            return true;
        }
        catch
        {
            return false;
        }
#endif
    }

    private static bool IsImmutable(Type type)
    {
        // check for known types
        if (type == typeof(string))
        {
            return true;
        }

        if (type.IsValueType)
        {
            // switch from Foo? to Foo if necessary
            if (Nullable.GetUnderlyingType(type) is { } nullable)
            {
                type = nullable;
            }
        }

        if (type.IsValueType || (type.IsClass & type.IsSealed))
        {
            // check for [ImmutableObject(true)]; note we're looking at this as a statement about
            // the overall nullability; for example, a type could contain a private int[] field,
            // where the field is mutable and the list is mutable; but if the type is annotated:
            // we're trusting that the API and use-case is such that the type is immutable
            return type.GetCustomAttribute<ImmutableObjectAttribute>() is { Immutable: true };
        }
        // don't trust interfaces and non-sealed types; we might have any concrete
        // type that has different behaviour
        return false;

    }
}
