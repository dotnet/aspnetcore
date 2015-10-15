// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class QueryFeature : IQueryFeature, IFeatureCache
    {
        private readonly IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpRequestFeature _request;

        private string _original;
        private IReadableStringCollection _parsedValues;

        public QueryFeature(IDictionary<string, StringValues> query)
            : this(new ReadableStringCollection(query))
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }
        }

        public QueryFeature(IReadableStringCollection query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            _parsedValues = query;
        }

        public QueryFeature(IFeatureCollection features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = features;
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision != _features.Revision)
            {
                _request = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _request); }
        }

        public IReadableStringCollection Query
        {
            get
            {
                if (_features == null)
                {
                    return _parsedValues ?? ReadableStringCollection.Empty;
                }

                var current = HttpRequestFeature.QueryString;
                if (_parsedValues == null || !string.Equals(_original, current, StringComparison.Ordinal))
                {
                    _original = current;
                    _parsedValues = new ReadableStringCollection(QueryHelpers.ParseQuery(current));
                }
                return _parsedValues;
            }
            set
            {
                _parsedValues = value;
                if (_features != null)
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