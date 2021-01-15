// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebUtilities;

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

                    var result = QueryHelpers.ParseNullableQuery(current);

                    if (result == null)
                    {
                        _parsedValues = QueryCollection.Empty;
                    }
                    else
                    {
                        _parsedValues = new QueryCollection(result);
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
    }
}
