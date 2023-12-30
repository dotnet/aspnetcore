// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IFeatureCollection"/>.
/// </summary>
[DebuggerDisplay("Count = {GetCount()}")]
[DebuggerTypeProxy(typeof(FeatureCollectionDebugView))]
public class FeatureCollection : IFeatureCollection
{
    private static readonly KeyComparer FeatureKeyComparer = new KeyComparer();
    private readonly IFeatureCollection? _defaults;
    private readonly int _initialCapacity;
    private IDictionary<Type, object>? _features;
    private volatile int _containerRevision;

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/>.
    /// </summary>
    public FeatureCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/> with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial number of elements that the collection can contain.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="initialCapacity"/> is less than 0</exception>
    public FeatureCollection(int initialCapacity)
    {
        ArgumentOutOfRangeThrowHelper.ThrowIfNegative(initialCapacity);

        _initialCapacity = initialCapacity;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureCollection"/> with the specified defaults.
    /// </summary>
    /// <param name="defaults">The feature defaults.</param>
    public FeatureCollection(IFeatureCollection defaults)
    {
        _defaults = defaults;
    }

    /// <inheritdoc />
    public virtual int Revision
    {
        get { return _containerRevision + (_defaults?.Revision ?? 0); }
    }

    /// <inheritdoc />
    public bool IsReadOnly { get { return false; } }

    /// <inheritdoc />
    public object? this[Type key]
    {
        get
        {
            ArgumentNullThrowHelper.ThrowIfNull(key);

            return _features != null && _features.TryGetValue(key, out var result) ? result : _defaults?[key];
        }
        set
        {
            ArgumentNullThrowHelper.ThrowIfNull(key);

            if (value == null)
            {
                if (_features != null && _features.Remove(key))
                {
                    _containerRevision++;
                }
                return;
            }

            if (_features == null)
            {
                _features = new Dictionary<Type, object>(_initialCapacity);
            }
            _features[key] = value;
            _containerRevision++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        if (_features != null)
        {
            foreach (var pair in _features)
            {
                yield return pair;
            }
        }

        if (_defaults != null)
        {
            // Don't return features masked by the wrapper.
            foreach (var pair in _features == null ? _defaults : _defaults.Except(_features, FeatureKeyComparer))
            {
                yield return pair;
            }
        }
    }

    /// <inheritdoc />
    public TFeature? Get<TFeature>()
    {
        if (typeof(TFeature).IsValueType)
        {
            var feature = this[typeof(TFeature)];
            if (feature is null && Nullable.GetUnderlyingType(typeof(TFeature)) is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TFeature).FullName} does not exist in the feature collection " +
                    $"and because it is a struct the method can't return null. Use 'featureCollection[typeof({typeof(TFeature).FullName})] is not null' to check if the feature exists.");
            }
            return (TFeature?)feature;
        }
        return (TFeature?)this[typeof(TFeature)];
    }

    /// <inheritdoc />
    public void Set<TFeature>(TFeature? instance)
    {
        this[typeof(TFeature)] = instance;
    }

    // Used by the debugger. Count over enumerable is required to get the correct value.
    private int GetCount() => this.Count();

    private sealed class KeyComparer : IEqualityComparer<KeyValuePair<Type, object>>
    {
        public bool Equals(KeyValuePair<Type, object> x, KeyValuePair<Type, object> y)
        {
            return x.Key.Equals(y.Key);
        }

        public int GetHashCode(KeyValuePair<Type, object> obj)
        {
            return obj.Key.GetHashCode();
        }
    }

    private sealed class FeatureCollectionDebugView(FeatureCollection features)
    {
        private readonly FeatureCollection _features = features;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DictionaryItemDebugView<Type, object>[] Items => _features.Select(pair => new DictionaryItemDebugView<Type, object>(pair)).ToArray();
    }
}
