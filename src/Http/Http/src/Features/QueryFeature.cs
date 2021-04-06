// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IQueryFeature"/>.
    /// </summary>
    public class QueryFeature : IQueryFeature
    {
        // Lambda hoisted to static readonly field to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IHttpRequestFeature?> _nullRequestFeature = f => null;

        private FeatureReferences<IHttpRequestFeature> _features;

        private string? _original;
        private IQueryCollection? _parsedValues;

        /// <summary>
        /// Initializes a new instance of <see cref="QueryFeature"/>.
        /// </summary>
        /// <param name="query">The <see cref="IQueryCollection"/> to use as a backing store.</param>
        public QueryFeature(IQueryCollection query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            _parsedValues = query;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryFeature"/>.
        /// </summary>
        /// <param name="features">The <see cref="IFeatureCollection"/> to initialize.</param>
        public QueryFeature(IFeatureCollection features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features.Initalize(features);
        }

        private IHttpRequestFeature HttpRequestFeature =>
            _features.Fetch(ref _features.Cache, _nullRequestFeature)!;

        /// <inheritdoc />
        public IQueryCollection Query
        {
            get
            {
                if (_features.Collection == null)
                {
                    if (_parsedValues == null)
                    {
                        _parsedValues = QueryCollection.Empty;
                    }
                    return _parsedValues;
                }

                var current = HttpRequestFeature.QueryString;
                if (_parsedValues == null || !string.Equals(_original, current, StringComparison.Ordinal))
                {
                    _original = current;

                    var result = ParseNullableQueryInternal(current);

                    if (result == null)
                    {
                        _parsedValues = QueryCollection.Empty;
                    }
                    else
                    {
                        _parsedValues = new QueryCollectionInternal(result);
                    }
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
        internal static AdaptiveCapacityDictionary<string, StringValues>? ParseNullableQueryInternal(string? queryString)
        {
            var accumulator = new KvpAccumulator();

            if (string.IsNullOrEmpty(queryString) || queryString == "?")
            {
                return null;
            }

            int scanIndex = 0;
            if (queryString[0] == '?')
            {
                scanIndex = 1;
            }

            int textLength = queryString.Length;
            int equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }
            while (scanIndex < textLength)
            {
                int delimiterIndex = queryString.IndexOf('&', scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }
                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    string name = queryString.Substring(scanIndex, equalIndex - scanIndex);
                    string value = queryString.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                    accumulator.Append(
                        Uri.UnescapeDataString(name.Replace('+', ' ')),
                        Uri.UnescapeDataString(value.Replace('+', ' ')));
                    equalIndex = queryString.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                else
                {
                    if (delimiterIndex > scanIndex)
                    {
                        accumulator.Append(queryString.Substring(scanIndex, delimiterIndex - scanIndex), string.Empty);
                    }
                }
                scanIndex = delimiterIndex + 1;
            }

            if (!accumulator.HasValues)
            {
                return null;
            }

            return accumulator.GetResults();
        }

        internal struct KvpAccumulator
        {
            /// <summary>
            /// This API supports infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            private AdaptiveCapacityDictionary<string, StringValues> _accumulator;
            private AdaptiveCapacityDictionary<string, List<string>> _expandingAccumulator;

            /// <summary>
            /// This API supports infrastructure and is not intended to be used
            /// directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public void Append(string key, string value)
            {
                if (_accumulator == null)
                {
                    _accumulator = new AdaptiveCapacityDictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
                }

                StringValues values;
                if (_accumulator.TryGetValue(key, out values))
                {
                    if (values.Count == 0)
                    {
                        // Marker entry for this key to indicate entry already in expanding list dictionary
                        _expandingAccumulator[key].Add(value);
                    }
                    else if (values.Count < 5)
                    {
                        _accumulator[key] = StringValues.Concat(values, value);
                    }
                    else
                    {
                        // Add zero count entry and move to data to expanding list dictionary
                        _accumulator[key] = default(StringValues);

                        if (_expandingAccumulator == null)
                        {
                            _expandingAccumulator = new AdaptiveCapacityDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                        }

                        // Already 5 entries so use starting allocated as 10; then use List's expansion mechanism for more
                        var list = new List<string>(10);

                        list.AddRange(values);
                        list.Add(value);

                        _expandingAccumulator[key] = list;
                    }
                }
                else
                {
                    // First value for this key
                    _accumulator[key] = new StringValues(value);
                }

                ValueCount++;
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
}
