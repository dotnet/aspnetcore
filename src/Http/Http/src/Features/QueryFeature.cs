// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IQueryFeature"/>.
/// </summary>
public class QueryFeature : IQueryFeature
{
    // Lambda hoisted to static readonly field to improve inlining https://github.com/dotnet/roslyn/issues/13624
    private static readonly Func<IFeatureCollection, IHttpRequestFeature?> _nullRequestFeature = f => null;

    private FeatureReferences<IHttpRequestFeature> _features;

    private string? _original;
    private IQueryCollection? _parsedValues;

    /// <summary>
    /// Initializes a new instance of <see cref="QueryFeature"/>.
    /// </summary>
    /// <param name="query">The <see cref="IQueryCollection"/> to use as a backing store.</param>
    public QueryFeature(IQueryCollection query)
    {
        ArgumentNullException.ThrowIfNull(query);

        _parsedValues = query;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="QueryFeature"/>.
    /// </summary>
    /// <param name="features">The <see cref="IFeatureCollection"/> to initialize.</param>
    public QueryFeature(IFeatureCollection features)
    {
        ArgumentNullException.ThrowIfNull(features);

        _features.Initalize(features);
    }

    private IHttpRequestFeature HttpRequestFeature =>
        _features.Fetch(ref _features.Cache, _nullRequestFeature)!;

    /// <inheritdoc />
    public IQueryCollection Query
    {
        get
        {
            if (_features.Collection is null)
            {
                return _parsedValues ?? QueryCollection.Empty;
            }

            var current = HttpRequestFeature.QueryString;
            if (_parsedValues == null || !string.Equals(_original, current, StringComparison.Ordinal))
            {
                _original = current;

                var result = ParseNullableQueryInternal(current);

                _parsedValues = result is not null
                    ? new QueryCollectionInternal(result)
                    : QueryCollection.Empty;
            }
            return _parsedValues;
        }
        set
        {
            _parsedValues = value;
            if (_features.Collection != null)
            {
                if (value == null)
                {
                    _original = string.Empty;
                    HttpRequestFeature.QueryString = string.Empty;
                }
                else
                {
                    _original = QueryString.Create(_parsedValues).ToString();
                    HttpRequestFeature.QueryString = _original;
                }
            }
        }
    }

    /// <summary>
    /// Parse a query string into its component key and value parts.
    /// </summary>
    /// <param name="queryString">The raw query string value, with or without the leading '?'.</param>
    /// <returns>A collection of parsed keys and values, null if there are no entries.</returns>
    [SkipLocalsInit]
    internal static AdaptiveCapacityDictionary<string, StringValues>? ParseNullableQueryInternal(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString) || (queryString.Length == 1 && queryString[0] == '?'))
        {
            return null;
        }

        var accumulator = new KvpAccumulator();
        var enumerable = new QueryStringEnumerable(queryString);
        foreach (var pair in enumerable)
        {
            accumulator.Append(pair.DecodeName().Span, pair.DecodeValue().Span);
        }

        return accumulator.HasValues
            ? accumulator.GetResults()
            : null;
    }

    internal struct KvpAccumulator
    {
        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        private AdaptiveCapacityDictionary<string, StringValues> _accumulator;
        private AdaptiveCapacityDictionary<string, List<string>> _expandingAccumulator;

        public void Append(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
            => Append(key.ToString(), value.ToString());

        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public void Append(string key, string value)
        {
            if (_accumulator is null)
            {
                _accumulator = new AdaptiveCapacityDictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            }

            if (!_accumulator.TryGetValue(key, out var values))
            {
                // First value for this key
                _accumulator[key] = new StringValues(value);
            }
            else
            {
                AppendToExpandingAccumulator(key, value, values);
            }

            ValueCount++;
        }

        private void AppendToExpandingAccumulator(string key, string value, StringValues values)
        {
            // When there are some values for the same key, so switch to expanding accumulator, and
            // add a zero count marker in the accumulator to indicate that switch.

            if (values.Count != 0)
            {
                _accumulator[key] = default;

                if (_expandingAccumulator is null)
                {
                    _expandingAccumulator = new AdaptiveCapacityDictionary<string, List<string>>(capacity: 5, StringComparer.OrdinalIgnoreCase);
                }

                // Already 2 (1 existing + the new one) entries so use List's expansion mechanism for more
                var list = new List<string>();

                list.AddRange(values);
                list.Add(value);

                _expandingAccumulator[key] = list;
            }
            else
            {
                // The marker indicates we are in the expanding accumulator, so just append to the list.
                _expandingAccumulator[key].Add(value);
            }
        }

        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool HasValues => ValueCount > 0;

        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public int KeyCount => _accumulator?.Count ?? 0;

        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public int ValueCount { get; private set; }

        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public AdaptiveCapacityDictionary<string, StringValues> GetResults()
        {
            if (_expandingAccumulator != null)
            {
                // Coalesce count 3+ multi-value entries into _accumulator dictionary
                foreach (var entry in _expandingAccumulator)
                {
                    _accumulator[entry.Key] = new StringValues(entry.Value.ToArray());
                }
            }

            return _accumulator ?? new AdaptiveCapacityDictionary<string, StringValues>(0, StringComparer.OrdinalIgnoreCase);
        }
    }
}
