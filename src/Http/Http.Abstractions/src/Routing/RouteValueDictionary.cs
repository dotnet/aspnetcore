// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;

[assembly: MetadataUpdateHandler(typeof(RouteValueDictionary.MetadataUpdateHandler))]

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// An <see cref="IDictionary{String, Object}"/> type for route values.
/// </summary>
[DebuggerTypeProxy(typeof(RouteValueDictionaryDebugView))]
[DebuggerDisplay("Count = {Count}")]
public class RouteValueDictionary : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
{
    private static readonly ConcurrentDictionary<Type, PropertyHelper[]> _propertyCache = new ConcurrentDictionary<Type, PropertyHelper[]>();

    internal DynamicArray<KeyValuePair<string, object?>> _arrayStorage;
    internal PropertyStorage? _propertyStorage;
    private int _count;

    /// <summary>
    /// Creates a new <see cref="RouteValueDictionary"/> from the provided Span of items.
    /// </summary>
    /// <param name="items">The items spans.</param>
    /// <returns>A new <see cref="RouteValueDictionary"/>.</returns>
    public static RouteValueDictionary FromValues(Span<KeyValuePair<string, object?>> items)
    {
        // We need to compress the array by removing non-contiguous items. We
        // typically have a very small number of items to process. We don't need
        // to preserve order.
        var start = 0;
        var end = items.Length - 1;

        // We walk forwards from the beginning of the array and fill in 'null' slots.
        // We walk backwards from the end of the array end move items in non-null' slots
        // into whatever start is pointing to. O(n)
        while (start <= end)
        {
            if (items[start].Key != null)
            {
                start++;
            }
            else if (items[end].Key != null)
            {
                // Swap this item into start and advance
                items[start] = items[end];
                items[end] = default;
                start++;
                end--;
            }
            else
            {
                // Both null, we need to hold on 'start' since we
                // still need to fill it with something.
                end--;
            }
        }

        return new RouteValueDictionary
        {
            _arrayStorage = new(items[..start]),
            _count = start,
        };
    }

    /// <summary>
    /// Creates a new <see cref="RouteValueDictionary"/> from the provided array.
    /// The new instance will take ownership of the array, and may mutate it.
    /// </summary>
    /// <param name="items">The items array.</param>
    /// <returns>A new <see cref="RouteValueDictionary"/>.</returns>
    public static RouteValueDictionary FromArray(KeyValuePair<string, object?>[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // We need to compress the array by removing non-contiguous items. We
        // typically have a very small number of items to process. We don't need
        // to preserve order.
        var start = 0;
        var end = items.Length - 1;

        // We walk forwards from the beginning of the array and fill in 'null' slots.
        // We walk backwards from the end of the array end move items in non-null' slots
        // into whatever start is pointing to. O(n)
        while (start <= end)
        {
            if (items[start].Key != null)
            {
                start++;
            }
            else if (items[end].Key != null)
            {
                // Swap this item into start and advance
                items[start] = items[end];
                items[end] = default;
                start++;
                end--;
            }
            else
            {
                // Both null, we need to hold on 'start' since we
                // still need to fill it with something.
                end--;
            }
        }

        return new RouteValueDictionary()
        {
            _arrayStorage = new(items!, start),
            _count = start,
        };
    }

    /// <summary>
    /// Creates an empty <see cref="RouteValueDictionary"/>.
    /// </summary>
    public RouteValueDictionary()
    {
    }

    /// <summary>
    /// Creates a <see cref="RouteValueDictionary"/> initialized with the specified <paramref name="values"/>.
    /// </summary>
    /// <param name="values">An object to initialize the dictionary. The value can be of type
    /// <see cref="IDictionary{TKey, TValue}"/> or <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// or an object with public properties as key-value pairs.
    /// </param>
    /// <remarks>
    /// If the value is a dictionary or other <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{String, Object}"/>,
    /// then its entries are copied. Otherwise the object is interpreted as a set of key-value pairs where the
    /// property names are keys, and property values are the values, and copied into the dictionary.
    /// Only public instance non-index properties are considered.
    /// </remarks>
    [RequiresUnreferencedCode("This constructor may perform reflection on the specificed value which may be trimmed if not referenced directly. Consider using a different overload to avoid this issue.")]
    public RouteValueDictionary(object? values)
    {
        if (values is RouteValueDictionary dictionary)
        {
            Initialize(dictionary);

            return;
        }

        if (values is IEnumerable<KeyValuePair<string, object?>> keyValueEnumerable)
        {
            Initialize(keyValueEnumerable);

            return;
        }

        if (values is IEnumerable<KeyValuePair<string, string?>> stringValueEnumerable)
        {
            Initialize(stringValueEnumerable);

            return;
        }

        if (values != null)
        {
            var storage = new PropertyStorage(values);
            _propertyStorage = storage;
            _count = storage.Properties.Length;
        }
    }

    /// <summary>
    /// Creates a <see cref="RouteValueDictionary"/> initialized with the specified <paramref name="values"/>.
    /// </summary>
    /// <param name="values">A sequence of values to add to the dictionary..</param>
    public RouteValueDictionary(IEnumerable<KeyValuePair<string, object?>>? values)
    {
        if (values is not null)
        {
            Initialize(values);
        }
    }

    /// <summary>
    /// Creates a <see cref="RouteValueDictionary"/> initialized with the specified <paramref name="values"/>.
    /// </summary>
    /// <param name="values">A sequence of values to add to the dictionary..</param>
    public RouteValueDictionary(IEnumerable<KeyValuePair<string, string?>>? values)
    {
        if (values is not null)
        {
            Initialize(values);
        }
    }

    /// <summary>
    /// Creates a <see cref="RouteValueDictionary"/> initialized with the specified <paramref name="dictionary"/>.
    /// </summary>
    /// <param name="dictionary">A <see cref="RouteValueDictionary"/> to initialize the dictionary.</param>
    public RouteValueDictionary(RouteValueDictionary? dictionary)
    {
        if (dictionary is not null)
        {
            Initialize(dictionary);
        }
    }

    private void Initialize(IEnumerable<KeyValuePair<string, string?>> stringValueEnumerable)
    {
        foreach (var kvp in stringValueEnumerable)
        {
            Add(kvp.Key, kvp.Value);
        }
    }

    private void Initialize(IEnumerable<KeyValuePair<string, object?>> keyValueEnumerable)
    {
        foreach (var kvp in keyValueEnumerable)
        {
            Add(kvp.Key, kvp.Value);
        }
    }

    private void Initialize(RouteValueDictionary dictionary)
    {
        if (dictionary._propertyStorage != null)
        {
            // PropertyStorage is immutable so we can just copy it.
            _propertyStorage = dictionary._propertyStorage;
            _count = dictionary._count;
            return;
        }

        var count = dictionary._count;
        if (count > 0)
        {
            ref var other = ref dictionary._arrayStorage;
            _arrayStorage = new(other.AsSpan());
            _count = count;
        }
    }

    /// <inheritdoc />
    public object? this[string key]
    {
        get
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            TryGetValue(key, out var value);
            return value;
        }

        set
        {
            if (key == null)
            {
                ThrowArgumentNullExceptionForKey();
            }

            // We're calling this here for the side-effect of converting from properties
            // to array. We need to create the array even if we just set an existing value since
            // property storage is immutable.
            EnsureCapacity();

            var index = FindIndex(key);
            if (index < 0)
            {
                _arrayStorage.Add(new KeyValuePair<string, object?>(key, value));
                _count++;
            }
            else
            {
                _arrayStorage[index] = new KeyValuePair<string, object?>(key, value);
            }
        }
    }

    /// <summary>
    /// Gets the comparer for this dictionary.
    /// </summary>
    /// <remarks>
    /// This will always be a reference to <see cref="StringComparer.OrdinalIgnoreCase"/>
    /// </remarks>
    public IEqualityComparer<string> Comparer => StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc />
    public int Count => _count;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<string> Keys
    {
        get
        {
            EnsureCapacity();

            var array = _arrayStorage.AsSpan();
            var keys = new string[_count];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = array[i].Key;
            }

            return keys;
        }
    }

    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => Keys;

    /// <inheritdoc />
    public ICollection<object?> Values
    {
        get
        {
            EnsureCapacity();

            var array = _arrayStorage.AsSpan();
            var values = new object?[_count];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = array[i].Value;
            }

            return values;
        }
    }

    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => Values;

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
    {
        Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public void Add(string key, object? value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        EnsureCapacity();

        if (ContainsKeyArray(key))
        {
            var message = Resources.FormatRouteValueDictionary_DuplicateKey(key, nameof(RouteValueDictionary));
            throw new ArgumentException(message, nameof(key));
        }

        _arrayStorage.Add(new KeyValuePair<string, object?>(key, value));
        _count++;
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_count == 0)
        {
            return;
        }

        if (_propertyStorage != null)
        {
            _arrayStorage.Clear();
            _propertyStorage = null;
            _count = 0;
            return;
        }

        _arrayStorage.Clear();
        _count = 0;
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
    {
        return TryGetValue(item.Key, out var value) && EqualityComparer<object>.Default.Equals(value, item.Value);
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        return ContainsKeyCore(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsKeyCore(string key)
    {
        if (_propertyStorage == null)
        {
            return ContainsKeyArray(key);
        }

        return ContainsKeyProperties(key);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, object?>>.CopyTo(
        KeyValuePair<string, object?>[] array,
        int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex > array.Length || array.Length - arrayIndex < Count)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (Count == 0)
        {
            return;
        }

        EnsureCapacity();

        var storage = _arrayStorage.AsSpan();
        storage.CopyTo(array.AsSpan(arrayIndex, Count));
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
    {
        if (Count == 0)
        {
            return false;
        }

        EnsureCapacity();

        var index = FindIndex(item.Key);
        ref var array = ref _arrayStorage;
        if (index >= 0 && EqualityComparer<object>.Default.Equals(array[index].Value, item.Value))
        {
            array.RemoveAt(index);
            _count--;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (Count == 0)
        {
            return false;
        }

        // Ensure property storage is converted to array storage as we'll be
        // applying the lookup and removal on the array
        EnsureCapacity();

        var index = FindIndex(key);
        if (index >= 0)
        {
            _count--;
            _arrayStorage.RemoveAt(index);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove and return the value that has the specified key from the <see cref="RouteValueDictionary"/>.
    /// </summary>
    /// <param name="key">The key of the element to remove and return.</param>
    /// <param name="value">When this method returns, contains the object removed from the <see cref="RouteValueDictionary"/>, or <c>null</c> if key does not exist.</param>
    /// <returns>
    /// <c>true</c> if the object was removed successfully; otherwise, <c>false</c>.
    /// </returns>
    public bool Remove(string key, out object? value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_count == 0)
        {
            value = default;
            return false;
        }

        // Ensure property storage is converted to array storage as we'll be
        // applying the lookup and removal on the array
        EnsureCapacity();

        var index = FindIndex(key);
        if (index >= 0)
        {
            _count--;

            ref var array = ref _arrayStorage;
            value = array[index].Value;
            array.RemoveAt(index);

            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to the add the provided <paramref name="key"/> and <paramref name="value"/> to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>Returns <c>true</c> if the value was added. Returns <c>false</c> if the key was already present.</returns>
    public bool TryAdd(string key, object? value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (ContainsKeyCore(key))
        {
            return false;
        }

        EnsureCapacity();
        _arrayStorage.Add(new KeyValuePair<string, object?>(key, value));
        _count++;
        return true;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value)
    {
        if (key == null)
        {
            ThrowArgumentNullExceptionForKey();
        }

        if (_propertyStorage == null)
        {
            return TryFindItem(key, out value);
        }

        return TryGetValueSlow(key, out value);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The constructor that would result in _propertyStorage being non-null is annotated with RequiresUnreferencedCodeAttribute. " +
        "We do not need to additionally produce an error in this method since it is shared by trimmer friendly code paths.")]
    private bool TryGetValueSlow(string key, out object? value)
    {
        if (_propertyStorage != null)
        {
            var storage = _propertyStorage;
            for (var i = 0; i < storage.Properties.Length; i++)
            {
                if (string.Equals(storage.Properties[i].Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = storage.Properties[i].GetValue(storage.Value);
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    [DoesNotReturn]
    private static void ThrowArgumentNullExceptionForKey()
    {
        throw new ArgumentNullException("key");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity()
    {
        if (_propertyStorage != null)
        {
            EnsureCapacitySlow();
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "The constructor that would result in _propertyStorage being non-null is annotated with RequiresUnreferencedCodeAttribute. " +
        "We do not need to additionally produce an error in this method since it is shared by trimmer friendly code paths.")]
    private void EnsureCapacitySlow()
    {
        Debug.Assert(_propertyStorage != null);

        var storage = _propertyStorage;

        // If we're converting from properties, it's likely due to an 'add' to make sure we have at least
        // the default amount of space.
        var array = new KeyValuePair<string, object?>[storage.Properties.Length];

        for (var i = 0; i < storage.Properties.Length; i++)
        {
            var property = storage.Properties[i];
            array[i] = new KeyValuePair<string, object?>(property.Name, property.GetValue(storage.Value));
        }

        _arrayStorage = new(array);
        _propertyStorage = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(string key)
    {
        // Generally the bounds checking here will be elided by the JIT because this will be called
        // on the same code path as EnsureCapacity.
        var array = _arrayStorage.AsSpan();
        var count = _count;

        for (var i = 0; i < count; i++)
        {
            if (string.Equals(array[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryFindItem(string key, out object? value)
    {
        var array = _arrayStorage.AsSpan();
        var count = _count;

        // Elide bounds check for indexing.
        if ((uint)count <= (uint)array.Length)
        {
            for (var i = 0; i < count; i++)
            {
                if (string.Equals(array[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = array[i].Value;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsKeyArray(string key)
    {
        var array = _arrayStorage.AsSpan();
        var count = _count;

        // Elide bounds check for indexing.
        if ((uint)count <= (uint)array.Length)
        {
            for (var i = 0; i < count; i++)
            {
                if (string.Equals(array[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsKeyProperties(string key)
    {
        Debug.Assert(_propertyStorage != null);

        var properties = _propertyStorage.Properties;
        for (var i = 0; i < properties.Length; i++)
        {
            if (string.Equals(properties[i].Name, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>
    {
        private readonly RouteValueDictionary _dictionary;
        private int _index;

        /// <summary>
        /// Instantiates a new enumerator with the values provided in <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">A <see cref="RouteValueDictionary"/>.</param>
        public Enumerator(RouteValueDictionary dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _dictionary = dictionary;

            Current = default;
            _index = 0;
        }

        /// <inheritdoc />
        public KeyValuePair<string, object?> Current { get; private set; }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Releases resources used by the <see cref="Enumerator"/>.
        /// </summary>
        public void Dispose()
        {
        }

        // Similar to the design of List<T>.Enumerator - Split into fast path and slow path for inlining friendliness
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var dictionary = _dictionary;

            // The uncommon case is that the propertyStorage is in use
            if (dictionary._propertyStorage == null && ((uint)_index < (uint)dictionary._count))
            {
                Current = dictionary._arrayStorage[_index];
                _index++;
                return true;
            }

            return MoveNextRare();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "The constructor that would result in _propertyStorage being non-null is annotated with RequiresUnreferencedCodeAttribute. " +
            "We do not need to additionally produce an error in this method since it is shared by trimmer friendly code paths.")]
        private bool MoveNextRare()
        {
            var dictionary = _dictionary;
            if (dictionary._propertyStorage != null && ((uint)_index < (uint)dictionary._count))
            {
                var storage = dictionary._propertyStorage;
                var property = storage.Properties[_index];
                Current = new KeyValuePair<string, object?>(property.Name, property.GetValue(storage.Value));
                _index++;
                return true;
            }

            _index = dictionary._count;
            Current = default;
            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            Current = default;
            _index = 0;
        }
    }

    [RequiresUnreferencedCode("This API is not trim safe - from PropertyHelper")]
    internal sealed class PropertyStorage
    {
        public readonly object Value;
        public readonly PropertyHelper[] Properties;

        public PropertyStorage(object value)
        {
            Debug.Assert(value != null);
            Value = value;

            // Cache the properties so we can know if we've already validated them for duplicates.
            var type = Value.GetType();
            if (!_propertyCache.TryGetValue(type, out Properties!))
            {
                Properties = PropertyHelper.GetVisibleProperties(type, allPropertiesCache: null, visiblePropertiesCache: null);
                ValidatePropertyNames(type, Properties);
                _propertyCache.TryAdd(type, Properties);
            }
        }

        private static void ValidatePropertyNames(Type type, PropertyHelper[] properties)
        {
            var names = new Dictionary<string, PropertyHelper>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                if (names.TryGetValue(property.Name, out var duplicate))
                {
                    var message = Resources.FormatRouteValueDictionary_DuplicatePropertyName(
                        type.FullName,
                        property.Name,
                        duplicate.Name,
                        nameof(RouteValueDictionary));
                    throw new InvalidOperationException(message);
                }

                names.Add(property.Name, property);
            }
        }
    }

    internal static class MetadataUpdateHandler
    {
        /// <summary>
        /// Invoked as part of <see cref="MetadataUpdateHandlerAttribute" /> contract for hot reload.
        /// </summary>
        internal static void ClearCache(Type[]? _)
        {
            _propertyCache.Clear();
        }
    }

    private sealed class RouteValueDictionaryDebugView(RouteValueDictionary dictionary)
    {
        private readonly RouteValueDictionary _dictionary = dictionary;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, object?>[] Items => _dictionary.ToArray();
    }
}
