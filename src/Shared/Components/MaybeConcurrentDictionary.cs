// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A dictionary that uses <see cref="ConcurrentDictionary{TKey, TValue}"/> when multithreading
/// is supported, and falls back to a plain <see cref="Dictionary{TKey, TValue}"/> on
/// single-threaded WASM. This helps the IL trimmer eliminate <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// and its dependencies from the output when targeting browser environments.
/// </summary>
internal interface IMaybeConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the value associated with the specified key, or adds a new value using the factory if the key doesn't exist.
    /// </summary>
    TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory);

    /// <summary>
    /// Gets the value associated with the specified key, or adds a new value using the factory if the key doesn't exist.
    /// </summary>
    TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument);

    /// <summary>
    /// Attempts to add the specified key and value.
    /// </summary>
    /// <returns><c>true</c> if the key/value pair was added; <c>false</c> if the key already exists.</returns>
    bool TryAdd(TKey key, TValue value);

    /// <summary>
    /// Attempts to remove and return the value with the specified key.
    /// </summary>
    bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value);
}

internal static class MaybeConcurrentDictionary
{
    /// <summary>
    /// Creates a new <see cref="IMaybeConcurrentDictionary{TKey, TValue}"/> appropriate for the current platform.
    /// On single-threaded WASM, returns a lightweight wrapper around <see cref="Dictionary{TKey, TValue}"/>.
    /// Otherwise, returns a wrapper around <see cref="ConcurrentDictionary{TKey, TValue}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMaybeConcurrentDictionary<TKey, TValue> Create<TKey, TValue>()
        where TKey : notnull
    {
        if (OperatingSystem.IsBrowser())
        {
            return new SingleThreadedDictionary<TKey, TValue>();
        }

        return new ConcurrentDictionaryWrapper<TKey, TValue>();
    }

    /// <summary>
    /// Creates a new <see cref="IMaybeConcurrentDictionary{TKey, TValue}"/> appropriate for the current platform,
    /// using the specified equality comparer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMaybeConcurrentDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer)
        where TKey : notnull
    {
        // TODO use RuntimeFeature.IsMultithreadingSupported when available (dotnet/runtime#124603)
        if (OperatingSystem.IsBrowser())
        {
            return new SingleThreadedDictionary<TKey, TValue>(comparer);
        }

        return new ConcurrentDictionaryWrapper<TKey, TValue>(comparer);
    }

    private sealed class SingleThreadedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IMaybeConcurrentDictionary<TKey, TValue>
        where TKey : notnull
    {
        public SingleThreadedDictionary()
        {
        }

        public SingleThreadedDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
        {
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (TryGetValue(key, out var existingValue))
            {
                return existingValue;
            }

            var newValue = valueFactory(key);

            // Re-check in case the factory added the same key (re-entrancy).
            if (TryGetValue(key, out existingValue))
            {
                return existingValue;
            }

            this[key] = newValue;

            return newValue;
        }

        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (TryGetValue(key, out var existingValue))
            {
                return existingValue;
            }

            var newValue = valueFactory(key, factoryArgument);

            // Re-check in case the factory added the same key (re-entrancy).
            if (TryGetValue(key, out existingValue))
            {
                return existingValue;
            }

            this[key] = newValue;

            return newValue;
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            ref var existingValue = ref CollectionsMarshal.GetValueRefOrAddDefault(this, key, out bool exists);
            if (!exists)
            {
                existingValue = value;
            }

            return !exists;
        }

        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return Remove(key, out value);
        }
    }

    private sealed class ConcurrentDictionaryWrapper<TKey, TValue> : IMaybeConcurrentDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _inner;

        public ConcurrentDictionaryWrapper()
        {
            _inner = new ConcurrentDictionary<TKey, TValue>();
        }

        public ConcurrentDictionaryWrapper(IEqualityComparer<TKey> comparer)
        {
            _inner = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        public TValue this[TKey key]
        {
            get => _inner[key];
            set => _inner[key] = value;
        }

        public ICollection<TKey> Keys => _inner.Keys;
        public ICollection<TValue> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) => _inner.GetOrAdd(key, valueFactory);
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument) => _inner.GetOrAdd(key, valueFactory, factoryArgument);
        public bool TryAdd(TKey key, TValue value) => _inner.TryAdd(key, value);
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value) => _inner.TryRemove(key, out value);
        public void Add(TKey key, TValue value) => ((IDictionary<TKey, TValue>)_inner).Add(key, value);
        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public bool Remove(TKey key) => _inner.TryRemove(key, out _);
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _inner.TryGetValue(key, out value);
        public void Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Add(item);
        public void Clear() => _inner.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Remove(item);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
