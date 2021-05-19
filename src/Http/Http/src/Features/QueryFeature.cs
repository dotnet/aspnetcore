// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.Internal;
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

            KvpAccumulator accumulator = new();
            var queryStringLength = queryString.Length;

            char[]? arryToReturnToPool = null;
            Span<char> query = (queryStringLength <= 128
                ? stackalloc char[128]
                : arryToReturnToPool = ArrayPool<char>.Shared.Rent(queryStringLength)
            ).Slice(0, queryStringLength);

            queryString.AsSpan().CopyTo(query);

            if (query[0] == '?')
            {
                query = query[1..];
            }

            while (!query.IsEmpty)
            {
                var delimiterIndex = query.IndexOf('&');

                var querySegment = delimiterIndex >= 0
                    ? query.Slice(0, delimiterIndex)
                    : query;

                var equalIndex = querySegment.IndexOf('=');

                if (equalIndex >= 0)
                {
                    var i = 0;
                    for (; i < querySegment.Length; ++i)
                    {
                        if (!char.IsWhiteSpace(querySegment[i]))
                        {
                            break;
                        }
                    }

                    var name = querySegment[i..equalIndex];
                    var value = querySegment.Slice(equalIndex + 1);

                    SpanHelper.ReplacePlusWithSpaceInPlace(name);
                    SpanHelper.ReplacePlusWithSpaceInPlace(value);

                    accumulator.Append(
                        Uri.UnescapeDataString(name.ToString()),
                        Uri.UnescapeDataString(value.ToString()));
                }
                else
                {
                    if (!querySegment.IsEmpty)
                    {
                        accumulator.Append(querySegment);
                    }
                }

                if (delimiterIndex < 0)
                {
                    break;
                }

                query = query.Slice(delimiterIndex + 1);
            }

            if (arryToReturnToPool is not null)
            {
                ArrayPool<char>.Shared.Return(arryToReturnToPool);
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

            public void Append(ReadOnlySpan<char> key, ReadOnlySpan<char> value = default)
                => Append(key.ToString(), value.IsEmpty ? string.Empty : value.ToString());

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

                if (_accumulator.TryGetValue(key, out var values))
                {
                    if (values.Count == 0)
                    {
                        // Marker entry for this key to indicate entry already in expanding list dictionary
                        _expandingAccumulator[key].Add(value);
                    }
                    else if (values.Count == 1)
                    {
                        _accumulator[key] = StringValues.Concat(values, value);
                    }
                    else
                    {
                        // Add zero count entry and move to data to expanding list dictionary
                        _accumulator[key] = default;

                        if (_expandingAccumulator is null)
                        {
                            _expandingAccumulator = new AdaptiveCapacityDictionary<string, List<string>>(5, StringComparer.OrdinalIgnoreCase);
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

        private static class SpanHelper
        {
            public static void ReplacePlusWithSpaceInPlace(Span<char> span)
                => ReplaceInPlace(span, '+', ' ');

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void ReplaceInPlace(Span<char> span, char oldChar, char newChar)
            {
                var i = (nint)0;
                var n = (nint)(uint)span.Length;

                fixed (char* ptr = span)
                {
                    var pVec = (ushort*)ptr;

                    if (Sse41.IsSupported && n >= Vector128<ushort>.Count)
                    {
                        var vecOldChar = Vector128.Create((ushort)oldChar);
                        var vecNewChar = Vector128.Create((ushort)newChar);

                        do
                        {
                            var vec = Sse2.LoadVector128(pVec + i);
                            var mask = Sse2.CompareEqual(vec, vecOldChar);
                            var res = Sse41.BlendVariable(vec, vecNewChar, mask);
                            Sse2.Store(pVec + i, res);

                            i += Vector128<ushort>.Count;
                        } while (i <= n - Vector128<ushort>.Count);
                    }

                    for (; i < n; ++i)
                    {
                        if (ptr[i] == oldChar)
                        {
                            ptr[i] = newChar;
                        }
                    }
                }
            }
        }
    }
}
